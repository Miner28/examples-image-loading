﻿using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class SlideshowFrame : UdonSharpBehaviour
{
    public VRCUrl[] imageUrls;
    public VRCUrl stringUrl;
    public new Renderer renderer;
    public Text field;
    public float slideDurationSeconds = 10f;
    
    // Private Variables
    private int _loadedIndex = -1;
    private VRCImageDownloader _imageDownloader;
    private IUdonEventReceiver udonEventReceiver;
    private string[] _captions = new string[0];
    private Texture2D[] _textures;
    
    void Start()
    {
        // Cast self to the type needed for Image and String Loading methods
        udonEventReceiver = (IUdonEventReceiver)this;
        
        // Track which downloads have been completed already
        // _downloadsComplete = new bool[rgbUrl.Length];
        _textures = new Texture2D[imageUrls.Length];
        
        // Construct Image Downloader to reuse
        _imageDownloader = new VRCImageDownloader();
        
        // Load Captions
        VRCStringDownloader.LoadUrl(stringUrl, udonEventReceiver);
        
        LoadNextRecursive();
    }

    public void LoadNextRecursive()
    {
        LoadNext();
        SendCustomEventDelayedSeconds(nameof(LoadNextRecursive), slideDurationSeconds);
    }
    
    private void LoadNext()
    {
        _loadedIndex = (int)(Networking.GetServerTimeInMilliseconds() / 1000f / slideDurationSeconds) % imageUrls.Length;

        var nextTexture = _textures[_loadedIndex];
        
        if (nextTexture != null)
        {
            renderer.sharedMaterial.mainTexture = nextTexture;
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, udonEventReceiver, rgbInfo);
        }

        // Set caption if one is provided
        if (_loadedIndex < _captions.Length)
        {
            field.text = _captions[_loadedIndex];
        }
        else
        {
            field.text = "";
        }
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        _captions = result.Result.Split('\n');
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        Debug.LogError($"Could not load string {result.Error}");
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        Debug.Log($"Image loaded: {result.SizeInMemoryBytes} bytes.");
        
        _textures[_loadedIndex] = result.Result;
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.Log($"Image not loaded: {result.Error.ToString()}: {result.ErrorMessage}.");
    }

    private void OnDestroy()
    {
        _imageDownloader.Dispose();
    }
}