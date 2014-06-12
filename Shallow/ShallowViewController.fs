namespace Shallow

open System
open System.Drawing

open MonoTouch.UIKit
open MonoTouch.Foundation

open Cirrious.FluentLayouts.Touch

[<Register ("ShallowViewController")>]
type ShallowViewController() as this =
    inherit UIViewController ()

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

    let swipeHandler = SwipeGestureHandler(content, photoView)

    override this.ViewDidLoad() =
        this.View <- content

    override this.ShouldAutorotateToInterfaceOrientation(orientation) =
        orientation <> UIInterfaceOrientation.PortraitUpsideDown