namespace Shallow

open System
open MonoTouch.UIKit
open MonoTouch.Foundation

[<Register ("AppDelegate")>]
type AppDelegate() =
    inherit UIApplicationDelegate()

    let window = UIWindow(UIScreen.MainScreen.Bounds)

    override this.FinishedLaunching(app, options) =
        window.RootViewController <- ShallowViewController()
        window.MakeKeyAndVisible()
        true

module Main =
    [<EntryPoint>]
    let main args =
        UIApplication.Main(args, null, "AppDelegate")
        0

