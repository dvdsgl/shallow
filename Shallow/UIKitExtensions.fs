[<AutoOpen>]
module MonoTouch.UIKit.Extensions

open System.Drawing
open MonoTouch.UIKit

open Cirrious.FluentLayouts.Touch

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