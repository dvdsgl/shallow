module EasyLayout

open System
open System.Drawing
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open MonoTouch.Foundation
open MonoTouch.UIKit

module private Utilities =

    let rec eval e =
        match e with
        | FieldGet (Some o, f) -> f.GetValue (eval o)
        | FieldGet (None, f) -> f.GetValue (null)
        | PropertyGet (None, p, i) -> p.GetValue (null, i |> Seq.map eval |> Seq.toArray)
        | PropertyGet (Some o, p, i) -> p.GetValue (eval o, i |> Seq.map eval |> Seq.toArray)
        | Value (x, _) -> x
        | _ -> raise (Exception (sprintf "Don't know how to eval %A" e))

    let toAttr m =
        match m with
        | "X" | "Left" -> NSLayoutAttribute.Left
        | "Y" | "Top" -> NSLayoutAttribute.Top
        | "Width" -> NSLayoutAttribute.Width
        | "Height" -> NSLayoutAttribute.Height
        | "Bottom" -> NSLayoutAttribute.Bottom
        | "Right" -> NSLayoutAttribute.Right
        | "CenterX" | "RectangleF.get_CenterX" -> NSLayoutAttribute.CenterX
        | "CenterY" | "RectangleF.get_CenterY" -> NSLayoutAttribute.CenterY
        | "Baseline" | "RectangleF.get_Baseline" -> NSLayoutAttribute.Baseline
        | _ -> NSLayoutAttribute.NoAttribute

    let isConstrainableProperty m = toAttr m <> NSLayoutAttribute.NoAttribute

    let (|GetFrameProp|) e =
        match e with
        | Let (_, PropertyGet (Some o, fn, _), PropertyGet (_, pn, _))
            when fn.Name = "Frame" && isConstrainableProperty pn.Name ->
            Some (eval o :?> NSObject, toAttr pn.Name)
        | Call (_, pn, [PropertyGet (Some o, fn, _)])
            when fn.Name = "Frame" && isConstrainableProperty pn.Name ->
            Some (eval o :?> NSObject, toAttr pn.Name)
        | _ -> None

    let compileLeftSide side =
        match side with
        | GetFrameProp (Some x) -> x
        | _ -> raise (Exception (sprintf "Left hand side of constraint is expected to be a UIView.Frame property. It was: %A" side))

    let (|Mul|) side =
        match side with
        | GetFrameProp (Some (x, p)) -> Some (x, p, 1.0f)
        | Call (_, m, [l; GetFrameProp (Some (x, p))]) when m.Name = "op_Multiply" -> Some (x, p, Convert.ToSingle (eval l))
        | Call (_, m, [GetFrameProp (Some (x, p)); l]) when m.Name = "op_Multiply" -> Some (x, p, Convert.ToSingle (eval l))
        | _ -> None

    let compileRightSide side =
        match side with
        | Mul (Some x) -> (Some x, 0.0f)
        | Call (_, mem, [Mul (Some x); c]) when mem.Name = "op_Addition" -> (Some x, Convert.ToSingle (eval c))
        | Call (_, mem, [Mul (Some x); c]) when mem.Name = "op_Subtraction" -> (Some x, -Convert.ToSingle (eval c))
        | Value (x, _) -> (None, Convert.ToSingle (x))
        | FieldGet _ -> (None, Convert.ToSingle (eval side))
        | _ -> raise (Exception (sprintf "Unrecognized right hand side: %A." side))

    let compileConstraint left right rel =
        let (firstObj, firstAttr) = compileLeftSide left
        let (maybeObj, add) = compileRightSide right
        match maybeObj with
        | None -> NSLayoutConstraint.Create (firstObj, firstAttr, rel, null, NSLayoutAttribute.NoAttribute, 0.0f, add)
        | Some (secObj, secAttr, mul) -> NSLayoutConstraint.Create (firstObj, firstAttr, rel, secObj, secAttr, mul, add)

    let toRel m =
        match m with
        | "op_Equality" -> Some NSLayoutRelation.Equal
        | "op_LessThanOrEqual" -> Some NSLayoutRelation.LessThanOrEqual
        | "op_GreaterThanOrEqual" -> Some NSLayoutRelation.GreaterThanOrEqual
        | _ -> None

    let rec compileConstraints expr  =
        match expr with
        | NewArray (_, es) -> es |> Seq.collect compileConstraints |> Seq.toList
        | IfThenElse (i, t, e) -> compileConstraints i @ compileConstraints t @ compileConstraints e
        | Call (_, m, [l; r]) when (toRel m.Name).IsSome -> [compileConstraint l r (toRel m.Name).Value]
        | Value _ -> []
        | _ -> raise (Exception (sprintf "Unable to recognize constraints in expression: %A" expr))

type RectangleF with
    member this.CenterX = this.X + this.Width / 2.0f
    member this.CenterY = this.Y + this.Height / 2.0f
    member this.Baseline = 0.0f

type UIView with
    /// <summary>
    /// <para>Constrains the layout of subviews according to equations and
    /// inequalities specified in <paramref name="constraints"/>.  Issue
    /// multiple constraints per call using the &amp;&amp; operator.</para>
    /// <code><@ button.Frame.Left &gt;= text.Frame.Right + 22 &amp;&amp;
    /// button.Frame.Width = View.Frame.Width * 0.42f @></code>
    /// </summary>
    /// <param name="constraints">Constraint equations and inequalities.</param>
    member this.ConstrainLayout (constraints) =
        let cs = Utilities.compileConstraints constraints |> Seq.toArray
        this.AddConstraints (cs)
        for x in cs do
            (x.FirstItem :?> UIView).TranslatesAutoresizingMaskIntoConstraints <- false
        cs
