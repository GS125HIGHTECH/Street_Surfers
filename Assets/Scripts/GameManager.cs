using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private async void Awake()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        SaveData();
    }

    public async void SaveData()
    {
        var playerData = new Dictionary<string, object>{
          {"name", "Grzegorz"},
          {"surname", "Sobczak"}
        };
        var result = await CloudSaveService.Instance.Data.Player.SaveAsync(playerData);
        Debug.Log($"Saved data {string.Join(',', playerData)}");
    }
}
