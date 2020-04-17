namespace PixivTerm
open System
open System.Net.Http
open TokenHandling
open Requests

module main =
    let logo = @"██████╗ ██╗██╗  ██╗██╗██╗   ██╗████████╗███████╗██████╗ ███╗   ███╗
██╔══██╗██║╚██╗██╔╝██║██║   ██║╚══██╔══╝██╔════╝██╔══██╗████╗ ████║
██████╔╝██║ ╚███╔╝ ██║██║   ██║   ██║   █████╗  ██████╔╝██╔████╔██║
██╔═══╝ ██║ ██╔██╗ ██║╚██╗ ██╔╝   ██║   ██╔══╝  ██╔══██╗██║╚██╔╝██║
██║     ██║██╔╝ ██╗██║ ╚████╔╝    ██║   ███████╗██║  ██║██║ ╚═╝ ██║
╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝     ╚═╝   ╚══════╝╚═╝  ╚═╝╚═╝     ╚═╝
                                                                   "
    
    // Logs in using either tokens or username+password
    let login tokenString =
        match tokenString with
        | "" ->
            printfn "username"
            let username = Console.ReadLine()
            printfn "password"
            let password = Console.ReadLine()
            Console.Clear()
            let response = loginRequest username password
            storeTokens [response.AccessToken; response.RefreshToken; response.DeviceToken]
            response
        | _ ->
            let tokenList = tokenString.Split ','
            let response = refreshLogin tokenList.[0] tokenList.[1] tokenList.[2]
            storeTokens [response.AccessToken; response.RefreshToken; response.DeviceToken]
            response
            
    // views illust
    let illust id =
        let response = viewIllust id
        let singlePageUrl = response.MetaSinglePage.OriginalImageUrl
        match singlePageUrl with
        | null -> viewAlbum response
        | _ -> viewSingleImage singlePageUrl
    
    // recommended illusts
    let recIllusts () =
        let illusts = recommended ()
        illusts |> List.iter (fun x -> printfn "%s - by %s | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                           (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // popular illusts search
    let popularIllusts searchTerm =
        let illusts = searchPopular searchTerm
        illusts |> List.iter (fun x -> printfn "%s - by %s | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                           (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // matches the command from input
    let matchCommand (command : string) =
        let commands = command.Split " "
        match commands.[0] with
        | "illust" -> illust commands.[1]
        | "recommended" -> recIllusts ()
        | "popular" -> popularIllusts commands.[1]
        | _ -> printfn "Command not found"
    
    // refreshes the tokens if needed
    let rec tryCommand command =
        try
            matchCommand command
        with
        | :? HttpRequestException as ex when ex.Message = "400" -> account <- login tokens; tryCommand command
        | :? AggregateException as ex when ex.InnerException.Message = "400" -> account <- login tokens; tryCommand command
        
    // main program loop
    let inputLoop () =
        printfn "%s" logo
        while true do
            printf "> "
            let input = Console.ReadLine()
            tryCommand input
        0
    
    // Main program 
    [<EntryPoint>]
    let main argv =
        readTokens ()
        match argv with
        | _ as args when args.Length = 0 -> inputLoop () |> ignore
        | _ ->
            let input = String.Join(" ", argv)
            tryCommand input
        0