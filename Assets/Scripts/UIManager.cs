using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TMP_Text coinCountText;

    private void Update()
    {
        long coinCount = GameManager.Instance.GetCoinCount();
        coinCountText.text = FormatNumber(coinCount);
    }

    private string FormatNumber(long number)
    {
        const long Quintillion = 1_000_000_000_000_000_000;
        const long Quadrillion = 1_000_000_000_000_000;
        const long Trillion = 1_000_000_000_000;
        const long Billion = 1_000_000_000;
        const long Million = 1_000_000;
        const long Thousand = 1_000;

        var cultureInfo = CultureInfo.CurrentCulture;
        string formattedNumber;

        Debug.Log("Current Culture Info: " + cultureInfo.Name);

        if (number >= Quintillion)
        {
            formattedNumber = (Math.Truncate((number / (double)Quintillion) * 100) / 100).ToString("0.##", cultureInfo) + "Qi";
        }
        else if (number >= Quadrillion)
        {
            formattedNumber = (Math.Truncate((number / (double)Quadrillion) * 100) / 100).ToString("0.##", cultureInfo) + "Qa";
        }
        else if (number >= Trillion)
        {
            formattedNumber = (Math.Truncate((number / (double)Trillion) * 100) / 100).ToString("0.##", cultureInfo) + "T";
        }
        else if (number >= Billion)
        {
            formattedNumber = (Math.Truncate((number / (double)Billion) * 100) / 100).ToString("0.##", cultureInfo) + "B";
        }
        else if (number >= Million)
        {
            formattedNumber = (Math.Truncate((number / (double)Million) * 100) / 100).ToString("0.##", cultureInfo) + "M";
        }
        else if (number >= Thousand)
        {
            formattedNumber = (Math.Truncate((number / (double)Thousand) * 100) / 100).ToString("0.##", cultureInfo) + "K";
        }
        else
        {
            formattedNumber = number.ToString("N0", cultureInfo);
        }

        return formattedNumber;
    }
}
