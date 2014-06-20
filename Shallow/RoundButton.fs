namespace Shallow

open System
open MonoTouch.UIKit

type RoundButton(size) as this =
    inherit UIButton(UIButtonType.Custom)

    do
        this.ConfigureBorder(width = 0.5, color = UIColor.Gray)
        this.Radius <- size / 2.0f

    member this.Radius with
        get () = this.Layer.CornerRadius
        and set radius = this.Layer.CornerRadius <- radius
