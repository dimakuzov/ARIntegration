using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using UnityEngine;

#if UNITY_IOS
public class NativeAPI {
    [DllImport("__Internal")]
    public static extern void showNativeApp();
    
    [DllImport("__Internal")]
    public static extern void giveDataFromAR(string mapID, string activity);
    
}
#endif

public class NativeCall : MonoBehaviour {
    
    //public Text messageFromIOS;
    
    
    public void BackButton() {
        NativeAPI.showNativeApp();
    }

    public void GiveDataButton() {
        string mapID = "mapID123";
        double lat = 1.23;
        double lon = 4.56;
        double alt = 7.89;
        double x = 0.12;
        double y = 3.45;
        double z = 6.78;
        
        string activity = "activityID123" + "," + "activityTitleTrivia" + "," 
                          + lat + "," + lon + "," + alt + "," + x + "," + y + "," + z;
        NativeAPI.giveDataFromAR(mapID, activity);
    }

    void GetMessageFromNativeIOS(string originalMessage) {
        char[] charSeparators = new char[] {','};
        string[] result = originalMessage.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
        
        //messageFromIOS.text = "NativeData" + "\n" + result[0] + "\n" + result[1];
    }
}


