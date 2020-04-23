namespace PixivTerm
open System
open System.Diagnostics
open System.IO
open PixivCSharp

module Requests =
    let client = PixivClient()
    let mutable account = LoginResponse()

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
        client.SearchIllustsAsync(String.Join(" ", target)) |> sendRequest |> fun x -> List.ofSeq x.Illusts
    
    // Gets a search result for popular illusts
    let searchPopular (target : string) =
        client.PopularIllustsPreviewAsync(String.Join(" ", target)) |> sendRequest |> fun x -> List.ofSeq x.Illusts
        
    // Gets recommended illusts
    let recommended () =
        client.RecommendedIllustsAsync() |> sendRequest |> fun x -> List.ofSeq x.Illusts
        
    // Gets ranking illusts
    let ranking mode (day : Nullable<DateTime>) =
        client.RankingIllustsAsync(mode, day) |> sendRequest |> fun x -> List.ofSeq x.Illusts
        
    // Methods for viewing and download images
    let viewIllust id =
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
            