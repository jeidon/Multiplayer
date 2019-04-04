﻿#region

using System.Linq;
using Harmony;
using Multiplayer.Common;
using RimWorld;
using UnityEngine;
using Verse;

#endregion

namespace Multiplayer.Client
{
    public class DisconnectedWindow : Window
    {
        public const float ButtonHeight = 40f;
        private readonly string desc;

        private readonly string reason;

        public bool returnToServerBrowser;

        public DisconnectedWindow(string reason, string desc = null)
        {
            this.reason = reason;
            this.desc = desc;

            if (reason.NullOrEmpty())
                reason = "Disconnected";

            closeOnAccept = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(300f, 150f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            Text.Anchor = TextAnchor.MiddleCenter;
            Rect labelRect = inRect;
            labelRect.yMax -= ButtonHeight;
            Widgets.Label(labelRect, desc.NullOrEmpty() ? reason : $"<b>{reason}</b>\n{desc}");
            Text.Anchor = TextAnchor.UpperLeft;

            DrawButtons(inRect);
        }

        public virtual void DrawButtons(Rect inRect)
        {
            float buttonWidth = Current.ProgramState == ProgramState.Entry ? 120f : 140f;
            Rect buttonRect = new Rect((inRect.width - buttonWidth) / 2f, inRect.height - ButtonHeight - 10f,
                buttonWidth, ButtonHeight);
            string buttonText = Current.ProgramState == ProgramState.Entry ? "CloseButton" : "QuitToMainMenu";

            if (Widgets.ButtonText(buttonRect, buttonText.Translate()))
            {
                if (Current.ProgramState == ProgramState.Entry)
                    Close();
                else
                    GenScene.GoToMainMenu();
            }
        }

        public override void PostClose()
        {
            if (returnToServerBrowser)
                Find.WindowStack.Add(new ServerBrowser());
        }
    }

    public class DefMismatchWindow : DisconnectedWindow
    {
        private readonly SessionModInfo mods;

        public DefMismatchWindow(SessionModInfo mods) : base("MpWrongDefs".Translate(), "MpWrongDefsInfo".Translate())
        {
            this.mods = mods;
            returnToServerBrowser = true;
        }

        public override Vector2 InitialSize => new Vector2(310f + 18 * 2, 160f);

        public override void DrawButtons(Rect inRect)
        {
            float btnWidth = 90f;
            float gap = 10f;

            Rect btnRect = new Rect(gap, inRect.height - ButtonHeight - 10f, btnWidth, ButtonHeight);

            if (Widgets.ButtonText(btnRect, "Details".Translate()))
            {
                string defs = mods.defInfo.Where(kv => kv.Value.status != DefCheckStatus.Ok)
                    .Join(kv => $"{kv.Key}: {kv.Value.status}", "\n");

                Find.WindowStack.Add(new TextAreaWindow($"Mismatches:\n\n{defs}"));
            }

            btnRect.x += btnWidth + gap;

            if (Widgets.ButtonText(btnRect, "MpModList".Translate()))
                ShowModList(mods);

            btnRect.x += btnWidth + gap;

            if (Widgets.ButtonText(btnRect, "CloseButton".Translate()))
                Close();
        }

        public static void ShowModList(SessionModInfo mods)
        {
            string activeMods = LoadedModManager.RunningModsListForReading.Join(m => "+ " + m.Name, "\n");
            string serverMods =
                mods.remoteModNames.Join(
                    name => (ModLister.AllInstalledMods.Any(m => m.Name == name) ? "+ " : "- ") + name, "\n");

            Find.WindowStack.Add(new TwoTextAreas_Window(
                $"RimWorld {mods.remoteRwVersion}\nServer mod list:\n\n{serverMods}",
                $"RimWorld {VersionControl.CurrentVersionString}\nActive mod list:\n\n{activeMods}"));
        }
    }
}