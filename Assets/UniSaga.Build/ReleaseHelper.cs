// Copyright @2022 COMCREATE. All rights reserved.

using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace UniSaga.Build
{
    public static class ReleaseHelper
    {
        [MenuItem("UniSaga/Release")]
        public static async void Release()
        {
            var request = Client.Pack("Assets/UniSaga", "output");
            var requestThreading = Client.Pack("Assets/UniSaga.Threading", "output");
            while (!request.IsCompleted || !requestThreading.IsCompleted)
            {
                await Task.Delay(1000);
            }

            if (request.Status == StatusCode.Success && requestThreading.Status == StatusCode.Success)
            {
                Debug.Log(
                    $"{request.Result}\n" +
                    "-----\n" +
                    $"{requestThreading.Result}"
                );
                return;
            }

            var errorMessage = string.Empty;
            if (request.Status != StatusCode.Success)
            {
                errorMessage += $"message:{request.Error.message}, errorCode:{request.Error.errorCode}";
            }

            if (requestThreading.Status != StatusCode.Success)
            {
                if (errorMessage.Length > 0) errorMessage += "\n-----\n";
                errorMessage +=
                    $"message:{requestThreading.Error.message}, errorCode:{requestThreading.Error.errorCode}";
            }

            Debug.LogError(errorMessage);
        }
    }
}