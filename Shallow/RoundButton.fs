namespace Shallow

open System
open MonoTouch.UIKit

type RoundButton(size) as this =
    inherit UIButton(UIButtonType.Custom)

    do
        this.Layer.BorderWidth <- 1.0f
        this.Layer.BorderColor <- UIColor.Gray.CGColor
        this.Radius <- size / 2.0f

    member this.Radius with
        get () = this.Layer.CornerRadius
        and set radius = this.Layer.CornerRadius <- radius
