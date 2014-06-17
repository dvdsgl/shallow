namespace Shallow

open System
open System.Drawing

open MonoTouch.UIKit

type Direction =
    Up | Down | Left | Right

    member this.UnitVector =
        match this with
        | Up -> (0.0f, 1.0f)
        | Down -> (0.0f, -1.0f)
        | Left -> (-1.0f, 0.0f)
        | Right -> (1.0f, 0.0f)

type ShootOutAnimator(referenceView: UIView) =
    let speed = 1300.0f
    let shotOut = Event<_>()
    let animator = UIDynamicAnimator(referenceView)

    member this.ViewShotOut = shotOut.Publish

    member this.ShootOut(view: UIView, direction: Direction) =
        let x, y = direction.UnitVector
        let dynamic = UIDynamicItemBehavior(view, AngularResistance = 2.0f)
        dynamic.AddLinearVelocityForItem(PointF(x * speed, y * speed), view)
        dynamic.AddAngularVelocityForItem(x * 3.0f, view)
        dynamic.Action <- fun () ->
            if not (referenceView.Bounds.IntersectsWith(view.Frame)) then
                animator.RemoveAllBehaviors()
                view.RemoveFromSuperview()
                shotOut.Trigger(view)
        animator.AddBehavior(dynamic)