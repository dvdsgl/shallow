namespace Shallow

open System
open System.Drawing
open System.Collections.Generic

open MonoTouch.UIKit

open MonoTouch.Foundation
open MonoTouch.CoreGraphics

open Cirrious.FluentLayouts.Touch

[<Register ("ShallowViewController")>]
type ShallowViewController() =
    inherit UIViewController()

    let showCodeUrl = "https://github.com/dvdsgl/shallow/blob/master/Shallow/ShallowViewController.fs"
    let photos = [
        "https://raw.githubusercontent.com/dvdsgl/shallow/master/data/photos/anuj.jpg"
        "https://raw.githubusercontent.com/dvdsgl/shallow/master/data/photos/julia.jpg"
        "https://raw.githubusercontent.com/dvdsgl/shallow/master/data/photos/kai.jpg"
        "https://raw.githubusercontent.com/dvdsgl/shallow/master/data/photos/karina.jpg"
    ]

    let nextPhoto =
        let current = ref 0
        let getImage = memoize UIImage.FromUrl
        async {
            let url = photos.[!current]
            current := (!current + 1) % photos.Length
            return getImage url
        }

    let photoSize = 300.0f
    let mainButtonSize = 75.0f
    let infoButtonSize = 45.0f
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
    let infoButton =
        let button = roundButton infoButtonSize "info-icon.png" 1.0f
        button.TouchUpInside.Add <| fun _ ->
            let alert = UIAlertView("Hello, F#!", "This lovely little app was written in F# with Xamarin.", null, "Cool", "👀 Code")
            alert.Clicked.Add <| fun args ->
                if args.ButtonIndex = 1 then
                    UIApplication.SharedApplication.OpenUrl(NSUrl(showCodeUrl))
                    ()
            alert.Show()
        button

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
        photoView.ImageAsync <- nextPhoto
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

    let shooter =
        let s = ShootOutAnimator(content)
        noButton.TouchUpInside.Add <| fun _ -> s.ShootOut(photoView, West)
        yesButton.TouchUpInside.Add <| fun _ -> s.ShootOut(photoView, East)
        s.ViewShotOut.Add <| fun _ ->
            resetPhotoView()
            content.AddSubview(photoView)
            popPhotoView()
        s

    override this.ViewDidLoad() =
        this.View <- content

    override this.ViewWillAppear(animated: bool) =
        base.ViewWillAppear(animated)
        resetPhotoView()

    override this.ShouldAutorotateToInterfaceOrientation(orientation) =
        orientation <> UIInterfaceOrientation.PortraitUpsideDown