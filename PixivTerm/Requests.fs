namespace PixivTerm
open System
open System.Diagnostics
open System.IO
open PixivCSharp
open TokenHandling

module Requests =
    let client = PixivClient()
    let mutable account = LoginResponse()
    let mutable nextUrl = String.Empty

    // Runs a task synchronously
    let sendRequest task =
        task |> Async.AwaitTask |> Async.RunSynchronously
        
    // Recieves input from the user to login
    let loginRequest username password =
        client.LoginAsync(username, password) |> sendRequest
        
    let refreshLogin access refresh device =
        client.SetTokens(access, refresh, device)
        client.RefreshLoginAsync() |> sendRequest
        
    // Get a search result
    let search (target : string) =
        let response = client.SearchIllustsAsync(String.Join(" ", target)) |> sendRequest
        nextUrl <- response.NextUrl
        response.Illusts |> List.ofSeq
        
    
    // Gets a search result for popular illusts
    let searchPopular (target : string) =
        let response = client.PopularIllustsPreviewAsync(String.Join(" ", target)) |> sendRequest
        nextUrl <- response.NextUrl
        response.Illusts |> List.ofSeq
        
    // Gets recommended illusts
    let recommended () =
        let response = client.RecommendedIllustsAsync() |> sendRequest
        nextUrl <- response.NextUrl
        response.Illusts |> List.ofSeq
        
    // Gets ranking illusts
    let ranking mode (day : Nullable<DateTime>) =
        let response = client.RankingIllustsAsync(mode, day) |> sendRequest
        nextUrl <- response.NextUrl
        response.Illusts |> List.ofSeq
        
    // Gets a list of bookmarks
    let bookmarksRequest () =
        let response = client.BookmarkedIllustsAsync (ID |> string) |> sendRequest
        nextUrl <- response.NextUrl
        response.Illusts |> List.ofSeq
        
    // Methods for viewing and download images
    let viewIllust id =
        nextUrl <- String.Empty
        client.ViewIllustAsync(id) |> sendRequest
    
    let saveImage filepath (url : string)  =
        async {
            let filename = url.Split "/" |> fun x -> x.[x.Length - 1]
            let! image = client.GetImageAsync(url) |> Async.AwaitTask
            let filestream = File.Create(filepath + filename)
            image.CopyTo(filestream)
            filestream.Close()
        }
    
    let viewSingleImage (url : string) =
        let filename = url.Split "/" |> fun x -> x.[x.Length - 1]
        saveImage "/tmp/" url |> Async.RunSynchronously
        let eog = Process.Start("eog", "/tmp/" + filename)
        eog.WaitForExit()
        File.Delete("/tmp/" + filename)
        
    let viewAlbum (illust : Illust) =
        let filepath = "/tmp/" + illust.ID.ToString() + "/"
        Directory.CreateDirectory(filepath) |> ignore
        let metaPages = illust.MetaPages |> List.ofSeq
        let urls = metaPages |> List.map (fun x -> x.ImageUrls.Original)
        let firstfilename = urls.[0].Split "/" |> fun x -> x.[x.Length - 1]
        
        urls |> List.map (saveImage filepath) |> Async.Parallel |> Async.RunSynchronously |> ignore
        
        let eog = Process.Start("eog", filepath + firstfilename)
        eog.WaitForExit()
        Directory.Delete("/tmp/" + illust.ID.ToString(), true)
            