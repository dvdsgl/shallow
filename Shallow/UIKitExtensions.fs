[<AutoOpen>]
module MonoTouch.UIKit.Extensions

open System.Drawing
open MonoTouch.UIKit
open MonoTouch.Foundation

open Cirrious.FluentLayouts.Touch

type UIView with
    /// Similar to UIView.AnimateAsync, but returns async rather than Task.
    static member AnimateAsync'(duration: float, animation: unit -> unit) = async {
        let! _ = UIView.AnimateAsync(duration, fun () -> animation()) |> Async.AwaitTask
        return ()
    }

    member this.ConfigureBorder(?color: UIColor, ?width: float, ?radius: float) =
        let layer = this.Layer
        color |> Option.iter (fun color -> layer.BorderColor <- color.CGColor)
        width |> Option.iter (fun width -> layer.BorderWidth <- float32 width)
        radius |> Option.iter (fun radius -> layer.CornerRadius <- float32 radius)

type UIImage with
    static member FromUrl(url) =
        url
        |> NSUrl.FromString
        |> NSData.FromUrl
        |> UIImage.LoadFromData

type UIImageView with
    /// Adds a spinning spinner to the middle of the view
    member private this.DisplaySpinner () =
        let spinner = UIActivityIndicatorView(
            UIActivityIndicatorViewStyle.Gray,
            TranslatesAutoresizingMaskIntoConstraints = false)
        this.Add(spinner)
        this.AddConstraints(spinner.WithSameCenterX(this), spinner.WithSameCenterY(this))
        spinner.StartAnimating()
        spinner

    /// Set the image asynchronously
    member this.ImageAsync with
        set getImage =
            let spinner = this.DisplaySpinner()
            let finished image =
                spinner.RemoveFromSuperview()
                spinner.StopAnimating()
                this.Image <- image

            async {
                let! image = getImage
                this.InvokeOnMainThread(fun () -> finished image)
            } |> Async.Start