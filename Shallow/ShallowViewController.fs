namespace Shallow

open System
open System.Drawing

open MonoTouch.UIKit

open MonoTouch.Foundation
open MonoTouch.CoreGraphics

open Cirrious.FluentLayouts.Touch

[<Register ("ShallowViewController")>]
type ShallowViewController() =
    inherit UIViewController()

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

    let photoView =
        let view = UIImageView(
            ClipsToBounds = true,
            UserInteractionEnabled = true,
            ContentMode = UIViewContentMode.ScaleAspectFit)
        view

    let content =
        let view = UIView(BackgroundColor = UIColor.White)

        view.AddSubviews(yesButton, noButton, infoButton, photoView)
        view.AddConstraints [|
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

    let resetPhotoView () =
        photoView.UserInteractionEnabled <- true
        photoView.Transform <- CGAffineTransform.MakeIdentity()
        photoView.Frame <- RectangleF(
            x = (content.Frame.Width - photoSize) / 2.0f,
            y = (content.Frame.Height - photoSize) / 2.0f - 50.0f,
            width = photoSize, height = photoSize)

    let popPhotoView () = Async.StartImmediate <| async {
        photoView.Transform <- CGAffineTransform.MakeScale(0.001f, 0.001f)
        do! Async.Sleep 100
        do! UIView.AnimateAsync'(0.3 / 1.5, fun () ->
            photoView.Transform <- CGAffineTransform.MakeScale(1.1f, 1.1f))
        do! UIView.AnimateAsync'(0.3 / 2.0, fun () ->
            photoView.Transform <- CGAffineTransform.MakeIdentity())
    }

    let swipeHandler =
        let handler = SwipeGestureHandler(content, photoView)
        handler.TargetViewRemoved.Add <| fun view ->
            resetPhotoView()
            content.AddSubview(photoView)
            popPhotoView()
        handler

    override this.ViewDidLoad() =
        this.View <- content
        photoView.ImageAsync <- async { return UIImage.FromUrl(photoUrl) }

    override this.ViewWillAppear(animated: bool) =
        base.ViewWillAppear(animated)

        // photoView is controlled by dynamics, so we don't use auto layout
        resetPhotoView()

    override this.ShouldAutorotateToInterfaceOrientation(orientation) =
        orientation <> UIInterfaceOrientation.PortraitUpsideDown