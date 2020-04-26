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
            ID <- response.User.ID
            storeTokens [response.AccessToken; response.RefreshToken; response.DeviceToken] (string ID)
            response
        | _ ->
            printfn "refreshing login"
            let tokenList = tokenString.Split ','
            let response = refreshLogin tokenList.[0] tokenList.[1] tokenList.[2]
            ID <- response.User.ID
            storeTokens [response.AccessToken; response.RefreshToken; response.DeviceToken] (string ID)
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
        illusts |> List.iter (fun x -> printfn "%s - by %s (%s) | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                           (x.User.ID |> string) (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    // searches illusts
    let searchIllusts searchTerm =
        let illusts = search searchTerm
        illusts |> List.iter (fun x -> printfn "%s - by %s (%s) | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                           (x.User.ID |> string) (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // popular illusts search
    let popularIllusts searchTerm =
        let illusts = searchPopular searchTerm
        illusts |> List.iter (fun x -> printfn "%s - by %s (%s) | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                           (x.User.ID |> string) (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // displays ranking illusts
    let rankingIllusts mode day =
        ranking mode day |> List.iter (fun x -> printfn "%s - by %s (%s) | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                                    (x.User.ID |> string) (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // bookmarks an illust
    let bookmark id restrict =
        client.AddBookmarkIllustAsync(id, restrict) |> Async.AwaitIAsyncResult |> Async.RunSynchronously
        
    // removes an illust bookmark
    let removeBookmark id =
        client.RemoveBookmarkIllustAsync(id) |> Async.AwaitIAsyncResult |> Async.RunSynchronously
        
    // views bookmarks
    let bookmarks () =
        bookmarksRequest () |> List.iter (fun x -> printfn "%s - by %s (%s)| %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                                    (x.User.ID |> string) (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
        
    // Follows a user
    let followUser user =
        client.FollowAsync(user) |> Async.AwaitIAsyncResult |> Async.RunSynchronously
    
    // Unfollows a user
    let unFollowUser user =
        client.RemoveFollowAsync(user) |> Async.AwaitIAsyncResult |> Async.RunSynchronously
        
    // Views followed users
    let following () =
        followingRequest () |> List.iter (fun x -> printfn "%s | ID - %s" x.User.Name (x.User.ID |> string))
        
    // gets the next page of a result
    let nextPage () =
        match nextUrl with
        | _ as x when x = "" || x = null -> printfn "No next URL"
        | _ ->
            let response = client.RequestAsync<IllustSearchResult>(nextUrl) |> sendRequest
            nextUrl <- response.NextUrl
            let responseList = response |> fun x -> List.ofSeq x.Illusts
            responseList |> List.iter (fun x -> printfn "%s - by %s | %s bookmarks | %s views | (ID - %s)" x.Title x.User.Name
                                                 (x.TotalBookmarks.ToString()) (x.TotalView.ToString()) (x.ID.ToString()))
    
    // matches the command from input
    let matchCommand (command : string) =
        let commandString = command.Replace("  ", " ")
        let commands = commandString.Split " "
        let args = [1..commands.Length-1] |> List.map (fun x -> commands.[x].Trim())
        match commands.[0].ToLower() with
        | "illust" ->
            if args.Length < 1 then printfn "No image ID provided" else
            illust (String.Join(" ", args))
        | "recommended" -> recIllusts ()
        | "popular" ->
            if args.Length < 1 then printfn "No tags provided" else
            popularIllusts (String.Join(" ", args))
        | "search" ->
            if args.Length < 1 then printfn "No tags provided" else
            searchIllusts (String.Join(" ", args))
        | "ranking" ->
            rankingIllusts
                (if args.Length > 0 then args.[0] else "day") 
                (if args.Length > 1 then Nullable<DateTime>(DateTime.Parse(args.[1])) else Nullable())
        | "bookmark" ->
            if args.Length < 1 then printfn "No image ID provided" else
            bookmark args.[0] "public" |> ignore
        | "unbookmark" ->
            if args.Length < 1 then printfn "No image ID provided" else
            removeBookmark args.[0] |> ignore
        | "bookmarks" -> bookmarks ()
        | "follow" ->
            if args.Length < 1 then printfn "No user ID provided" else
            followUser args.[0] |> ignore
        | "unfollow" ->
            if args.Length < 1 then printfn "No user ID provided" else
            unFollowUser args.[0] |> ignore
        | "following" -> following ()
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
        | :? AggregateException as ex when ex.InnerException.Message = "404" -> printfn "403 not found"
        | :? AggregateException as ex when ex.InnerException.Message = "400" -> printfn "an error occured %s" ex.Message
        | _ -> printfn "An error occured"
        
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