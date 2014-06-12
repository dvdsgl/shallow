namespace Shallow

open System
open System.Drawing

open MonoTouch.UIKit
open MonoTouch.Foundation

open Cirrious.FluentLayouts.Touch

type RoundButton(size) as this =
    inherit UIButton(UIButtonType.Custom)

    do
        this.Layer.BorderWidth <- 1.0f
        this.Layer.BorderColor <- UIColor.Gray.CGColor
        this.Radius <- size / 2.0f

    member this.Radius with
        get () = this.Layer.CornerRadius
        and set radius = this.Layer.CornerRadius <- radius

[<Register ("ShallowViewController")>]
type ShallowViewController() as this =
    inherit UIViewController ()

    // State for gesture recognizer
    let mutable attachment: UIAttachmentBehavior option = None
    let mutable startCenter = PointF.Empty
    let mutable lastTime = DateTime.MinValue 
    let mutable lastAngle = 0.0f
    let mutable angularVelocity = 0.0f

    let photoUrl = "https://dl.dropboxusercontent.com/u/217582/xamarin/shallow/david.jpg"

    let photoSize = 300.0f
    let mainButtonSize = 75.0f
    let infoButtonSize = 35.0f
    let buttonBetweenSpacing = 5.0f
    let buttonBottomSpacing = 50.0f

    let roundButton size image inset =
        let button = RoundButton(
            size,
            ImageEdgeInsets = UIEdgeInsets(inset, 0.0f, 0.0f, 0.0f),
            TranslatesAutoresizingMaskIntoConstraints = false)
        button.SetImage(UIImage.FromBundle(image), UIControlState.Normal)
        button

    let yesButton = roundButton mainButtonSize "like-icon.png" 4.0f
    let noButton = roundButton mainButtonSize "nope-icon.png" 1.0f
    let infoButton = roundButton infoButtonSize "info-icon.png" 1.0f

    let angleOfView (view: #UIView) =
        // http://stackoverflow.com/a/2051861/1271826
        // https://github.com/mono/maccore/blob/master/src/CoreGraphics/CGAffineTransform.cs#L39
        atan2 view.Transform.yx view.Transform.xx

    let downloadImage url = async {
        let data = url |> NSUrl.FromString |> NSData.FromUrl
        return UIImage.LoadFromData(data)
    }

    let photoView =
        let view = UIImageView(
            BackgroundColor = UIColor.Red,
            UserInteractionEnabled = true,
            TranslatesAutoresizingMaskIntoConstraints = false)
        view.Image <- downloadImage photoUrl |> Async.RunSynchronously

        let pan = UIPanGestureRecognizer(this.HandlePan)
        view.AddGestureRecognizer(pan)

        view

    let content =
        let view = UIView(
            BackgroundColor = UIColor.White)

        view.AddSubviews(yesButton, noButton, infoButton, photoView)
        view.AddConstraints [|
            photoView.Width().EqualTo(photoSize)
            photoView.Height().EqualTo(photoSize)
            //photoView.WithSameCenterX(view)
            //photoView.WithSameCenterY(view).Minus(50.0f)

            infoButton.Width().EqualTo(infoButtonSize)
            infoButton.Height().EqualTo(infoButtonSize)
            infoButton.WithSameBottom(view).Minus(buttonBottomSpacing)
            infoButton.WithSameCenterX(view)

            yesButton.Width().EqualTo(mainButtonSize)
            yesButton.Height().EqualTo(mainButtonSize)
            yesButton.WithSameCenterY(infoButton)
            yesButton.ToRightOf(infoButton, buttonBetweenSpacing)

            noButton.Width().EqualTo(mainButtonSize)
            noButton.Height().EqualTo(mainButtonSize)
            noButton.WithSameCenterY(infoButton)
            noButton.ToLeftOf(infoButton, buttonBetweenSpacing)
        |]
        view

    let animator = UIDynamicAnimator(content)

    override this.ViewDidLoad() =
        this.View <- content

    override this.ShouldAutorotateToInterfaceOrientation(orientation) =
        orientation <> UIInterfaceOrientation.PortraitUpsideDown

    // http://stackoverflow.com/questions/21325057/implement-uikitdynamics-for-dragging-view-off-screen
    member this.HandlePan (gesture: UIPanGestureRecognizer) =
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