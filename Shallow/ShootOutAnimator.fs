namespace Shallow

open System
open System.Drawing

open MonoTouch.UIKit

type Direction =
    North | East | South | West

    member this.UnitVector =
        match this with
        | North -> PointF(0.0f, 1.0f)
        | South -> PointF(0.0f, -1.0f)
        | West -> PointF(-1.0f, 0.0f)
        | East -> PointF(1.0f, 0.0f)

type ShootOutAnimator(referenceView: UIView) =
    let speed = 1300.0f
    let shotOut = Event<_>()
    let animator = UIDynamicAnimator(referenceView)

    member this.ViewShotOut = shotOut.Publish

    member this.ShootOut(view: UIView, direction: Direction) =
        let dynamic = UIDynamicItemBehavior(view, AngularResistance = 2.0f)
        let x, y = direction.UnitVector.X, direction.UnitVector.Y
       
        dynamic.AddLinearVelocityForItem(PointF(x * speed, y * speed), view)
        dynamic.AddAngularVelocityForItem(x * 3.0f, view)
        dynamic.Action <- fun () ->
            if not (referenceView.Bounds.IntersectsWith(view.Frame)) then
                animator.RemoveAllBehaviors()
                view.RemoveFromSuperview()
                shotOut.Trigger(view)
        animator.AddBehavior(dynamic)
        ()