namespace Shallow

open System
open System.Drawing

open MonoTouch.UIKit

// http://stackoverflow.com/questions/21325057/implement-uikitdynamics-for-dragging-view-off-screen
type SwipeGestureHandler(referenceView: UIView, targetView: UIView) =
    let mutable attachment = None
    let mutable startCenter = PointF.Empty
    let mutable lastTime = DateTime.MinValue 
    let mutable lastAngle = 0.0f
    let mutable angularVelocity = 0.0f

    let animator = UIDynamicAnimator(referenceView)

    let angleOfView (view: #UIView) =
        // http://stackoverflow.com/a/2051861/1271826 and
        // https://github.com/mono/maccore/blob/master/src/CoreGraphics/CGAffineTransform.cs#L39
        atan2 view.Transform.yx view.Transform.xx

    let handlePan (gesture: UIPanGestureRecognizer) =
        let view, superview = gesture.View, gesture.View.Superview
        match gesture.State with
        | UIGestureRecognizerState.Began ->
            animator.RemoveAllBehaviors()
            startCenter <- view.Center

            let offset =
                let size = view.Bounds.Size
                // calculate the center offset and anchor point
                let pointWithinAnimatedView = gesture.LocationInView(view)
                UIOffset(pointWithinAnimatedView.X - size.Width / 2.0f, pointWithinAnimatedView.Y- size.Height / 2.0f)
            let anchor = gesture.LocationInView(superview)

            // calculate angular velocity
            lastTime <- DateTime.Now
            lastAngle <- angleOfView view

            // create attachment behavior
            attachment <-
                let attach = UIAttachmentBehavior(view, offset, anchor)
                attach.Action <- fun () ->
                    let time = DateTime.Now
                    let angle = angleOfView view
                    if time > lastTime then
                        let seconds = float32 (time - lastTime).TotalSeconds
                        angularVelocity <- (angle - lastAngle) / seconds
                        lastTime <- time
                        lastAngle <- angle
                animator.Add(attach)
                Some attach

        | UIGestureRecognizerState.Changed ->
            // as user makes gesture, update attachment behavior's anchor point, achieving drag 'n' rotate
            attachment |> Option.iter (fun attachment ->
                attachment.AnchorPoint <- gesture.LocationInView(superview))

        | UIGestureRecognizerState.Ended ->
            animator.RemoveBehaviors()
            let velocity = gesture.VelocityInView(superview)

            // if we aren't dragging it down, just snap it back and quit
            let pi = float32 Math.PI
            if abs ((atan2 velocity.Y velocity.X) - pi / 2.0f) > pi / 4.0f then
                animator.AddBehavior(UISnapBehavior(view, startCenter))

            // otherwise, create UIDynamicItemBehavior that carries on animation from where
            // the gesture left off (notably linear and angular velocity)
            else
                let dynamic = UIDynamicItemBehavior(view, AngularResistance = 2.0f)
                dynamic.AddLinearVelocityForItem(velocity, view)
                dynamic.AddAngularVelocityForItem(angularVelocity, view)

                // when the view no longer intersects with its superview, go ahead and remove it
                dynamic.Action <- fun () ->
                    if not (superview.Bounds.IntersectsWith(view.Frame)) then
                        animator.RemoveAllBehaviors()
                        view.RemoveFromSuperview()
                
                animator.AddBehavior(dynamic)
                // add a little gravity so it accelerates off the screen (in case user gesture was slow)
                animator.AddBehavior(UIGravityBehavior(view, Magnitude = 0.7f))

    do targetView.AddGestureRecognizer(UIPanGestureRecognizer(handlePan))

