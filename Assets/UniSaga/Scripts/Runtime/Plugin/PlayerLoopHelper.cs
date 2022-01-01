// Copyright @2021 COMCREATE. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.LowLevel;
using PlayerLoopType = UnityEngine.PlayerLoop;
#else
using UnityEngine.Experimental.LowLevel;
using PlayerLoopType = UnityEngine.Experimental.PlayerLoop;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniSaga.Plugin
{
    internal static class UniSagaLoopRunners
    {
        public struct UniSagaDirectorUpdate
        {
        }
    }

    internal interface IPlayerLoopItem
    {
        bool MoveNext();
    }

    internal static class PlayerLoopHelper
    {
        [CanBeNull] private static PlayerLoopRunner _runner;

        public static void AddAction([NotNull] IPlayerLoopItem action)
        {
            if (_runner == null) throw new InvalidOperationException();
            _runner.AddAction(action);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
            // When domain reload is disabled, re-initialization is required when entering play mode; 
            // otherwise, pending tasks will leak between play mode sessions.
            var domainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled &&
                                       EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions
                                           .DisableDomainReload);
            if (!domainReloadDisabled && _runner != null) return;
#else
            if (_runner != null) return; // already initialized
#endif

            var playerLoop =
#if UNITY_2019_3_OR_NEWER
                PlayerLoop.GetCurrentPlayerLoop();
#else
                PlayerLoop.GetDefaultPlayerLoop();
#endif

            Initialize(ref playerLoop);
        }

        private static void Initialize(ref PlayerLoopSystem playerLoop)
        {
            var uniSagaLoopRunnerUpdateType = typeof(UniSagaLoopRunners.UniSagaDirectorUpdate);
            var copyList = playerLoop.subSystemList;
            var i = FindLoopSystemIndex(copyList, typeof(PlayerLoopType.Update));
            copyList[i].subSystemList = InsertRunner(
                copyList[i],
                uniSagaLoopRunnerUpdateType,
                _runner = new PlayerLoopRunner()
            );
            playerLoop.subSystemList = copyList;
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        private static PlayerLoopSystem[] InsertRunner(
            PlayerLoopSystem updateLoopSystem,
            [NotNull] Type uniSagaLoopRunnerUpdateType,
            [NotNull] PlayerLoopRunner runner
        )
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state != PlayModeStateChange.EnteredEditMode &&
                    state != PlayModeStateChange.ExitingEditMode) return;
                // run rest action before clear.
                if (_runner == null) return;
                _runner.Run();
                _runner.Clear();
            };
#endif
            var runnerLoop = new PlayerLoopSystem()
            {
                type = uniSagaLoopRunnerUpdateType,
                updateDelegate = runner.Run
            };
            var source = updateLoopSystem.subSystemList
                .Where(system => system.GetType() != uniSagaLoopRunnerUpdateType).ToArray();
            var dest = new List<PlayerLoopSystem>(source);
            var i = dest.FindIndex(x => x.type.Name == "ScriptRunDelayedTasks");
            if (i < 0) throw new InvalidOperationException();
            dest.Insert(i + 1, runnerLoop);
            return dest.ToArray();
        }

        private static int FindLoopSystemIndex(IList<PlayerLoopSystem> playerLoopList, Type systemType)
        {
            for (var i = 0; i < playerLoopList.Count; i++)
            {
                if (playerLoopList[i].type == systemType)
                {
                    return i;
                }
            }

            throw new Exception("Target PlayerLoopSystem does not found. Type:" + systemType.FullName);
        }
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitOnEditor()
        {
            // Execute the play mode init method
            Init();

            // register an Editor update delegate, used to forcing playerLoop update
            EditorApplication.update += ForceEditorPlayerLoopUpdate;
        }

        private static void ForceEditorPlayerLoopUpdate()
        {
            if (
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating
            )
            {
                return;
            }

            _runner?.Run();
        }
#endif

#if UNITY_2019_3_OR_NEWER
        public static void DumpCurrentPlayerLoop()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"PlayerLoop List");
            foreach (var header in playerLoop.subSystemList)
            {
                sb.AppendFormat("------{0}------", header.type.Name);
                sb.AppendLine();
                foreach (var subSystem in header.subSystemList)
                {
                    sb.AppendFormat("{0}", subSystem.type.Name);
                    sb.AppendLine();

                    if (subSystem.subSystemList != null)
                    {
                        Debug.LogWarning("More Subsystem:" + subSystem.subSystemList.Length);
                    }
                }
            }

            Debug.Log(sb.ToString());
        }
#endif
    }
}