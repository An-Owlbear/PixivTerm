namespace PixivTerm
open System
open System.Net.Http
open PixivCSharp
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
            printfn "refreshing login"
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
    // searches illusts
    let searchIllusts searchTerm =
        let illusts = search searchTerm
        illusts |> List.iter (fun x -> printfn "%s - by %s | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                           (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // popular illusts search
    let popularIllusts searchTerm =
        let illusts = searchPopular searchTerm
        illusts |> List.iter (fun x -> printfn "%s - by %s | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                           (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // displays ranking illusts
    let rankingIllusts mode day =
        ranking mode day |> List.iter (fun x -> printfn "%s - by %s | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                                    (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // bookmarks an illust
    let bookmark id restrict =
        client.AddBookmarkIllustAsync(id, restrict) |> Async.AwaitIAsyncResult |> Async.RunSynchronously
        
    // removes an illust bookmark
    let removeBookmark id =
        client.RemoveBookmarkIllustAsync(id) |> Async.AwaitIAsyncResult |> Async.RunSynchronously
        
    // views bookmarks
    let bookmarks () =
        bookmarksRequest () |> List.iter (fun x -> printfn "%s - by %s | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                                    (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
        
    // gets the next page of a result
    let nextPage () =
        match nextUrl with
        | _ as x when x = "" || x = null -> printfn "No next URL"
        | _ ->
            let response = client.RequestAsync<IllustSearchResult>(nextUrl) |> sendRequest |> fun x -> List.ofSeq x.Illusts
            response |> List.iter (fun x -> printfn "%s - by %s | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                                (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // matches the command from input
    let matchCommand (command : string) =
        let commandString = command.Replace("  ", " ")
        let commands = commandString.Split " "
        let args = [1..commands.Length-1] |> List.map (fun x -> commands.[x].Trim())
        match commands.[0].ToLower() with
        | "illust" -> illust (String.Join(" ", args))
        | "recommended" -> recIllusts ()
        | "popular" -> popularIllusts (String.Join(" ", args))
        | "search" -> searchIllusts (String.Join(" ", args))
        | "ranking" ->
            rankingIllusts
                (if args.Length > 0 then args.[0] else "day") 
                (if args.Length > 1 then Nullable<DateTime>(DateTime.Parse(args.[1])) else Nullable())
        | "bookmark" -> bookmark args.[0] "public" |> ignore
        | "unbookmark" -> removeBookmark args.[0] |> ignore
        | "bookmarks" -> bookmarks ()
        | "next" ->  nextPage ()
        | "exit" -> Environment.Exit(0)
        | _ -> printfn "Command not found"
    
    // refreshes the tokens if needed
    let rec tryCommand command =
        try
            matchCommand command
        with
        | :? HttpRequestException as ex when ex.Message = "Authentication error" -> account <- login tokens; tryCommand command
        | :? AggregateException as ex when ex.InnerException.Message = "Authentication error" -> account <- login tokens; tryCommand command
        | :? AggregateException as ex when ex.InnerException.Message = "400" -> printfn "an error occured"
        | ex -> printfn "An error occured"
        
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
        
        if tokens <> String.Empty then    
            let tokenList = tokens.Split ","
            client.SetTokens(tokenList.[0], tokenList.[1], tokenList.[2])
        
        match argv with
        | _ as args when args.Length = 0 -> inputLoop () |> ignore
        | _ ->
            let input = String.Join(" ", argv)
            tryCommand input
        0