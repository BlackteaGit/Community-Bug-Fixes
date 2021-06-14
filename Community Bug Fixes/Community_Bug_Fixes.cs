﻿using System;
using System.Collections.Generic;
using HarmonyLib;
using CoOpSpRpG;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WTFModLoader;
using WTFModLoader.Manager;

namespace Community_Bug_Fixes
{
	public class Community_Bug_Fixes : IWTFMod
	{
		public ModLoadPriority Priority => ModLoadPriority.Low;
		public void Initialize()
		{
			Harmony harmony = new Harmony("blacktea.Community_Bug_Fixes");
			harmony.PatchAll();
		}

		//public static TriggerEvent thrown;

		//fixing: monster getting invisible an lagging the game after quiting the game near monsters and reloading (in vanilla some animations fail to instantiate for some reason)
		[HarmonyPatch(typeof(Monster), "animateMovement")]
		public class Monster_animateMovement
		{
			[HarmonyPrefix]
			private static bool Prefix(Monster __instance, SheetAnimation ___idleAnim, SheetAnimation ___walkingAnim, List<SheetAnimation> ___attackAnims, SheetAnimation ___deathAnim, SheetAnimation ___currentAnim)
			{
				if (__instance.animState == MonsterState.idle && ___walkingAnim != null)
				{
					__instance.animState = MonsterState.moving;
				}
				else if (___walkingAnim == null)
				{
					___idleAnim = new SheetAnimation(120, 120, 9, 49, 0.033333335f, 28, true);
					___walkingAnim = new SheetAnimation(120, 120, 9, 0, 0.033333335f, 24, true);
					___attackAnims = new List<SheetAnimation>();
					___attackAnims.Add(new SheetAnimation(120, 120, 9, 25, 0.033333335f, 11, false, 0.26666668f));
					___attackAnims.Add(new SheetAnimation(120, 120, 9, 37, 0.033333335f, 11, false, 0.26666668f));
					___currentAnim = ___idleAnim;
				}
				return false; //instruction for harmony to supress executing the original method
			}
		}

		//fixing: game crash on spawning Budds ship. (in vanilla KillBuddQuestRev2.targets is not initiallized on adding values to it )
		[HarmonyPatch(typeof(KillBuddQuestRev2), "test")]
		public class KillBuddQuestRev2_test
		{
			[HarmonyPrefix]
			private static void Prefix(KillBuddQuestRev2 __instance, ulong ___buddID)
			{
				if (___buddID != 0UL && PLAYER.currentSession.allShips.ContainsKey(___buddID))
				{
					if (__instance.targets == null)
					{
						__instance.targets = new List<ulong>();
					}
				}	
			}	
		}


		//In vanilla the exception handling code throws exception itself on removing quests from active list while iterating on it
		//sadly harmony "finalizer" patch not working for some reason, will think of another way to fix it until it will be fixed in vanilla
		/* 
		[HarmonyPatch(typeof(TriggerEvent), "test")]
		public class TriggerEvent_test
		{
			[HarmonyFinalizer]
			static Exception Finalizer(TriggerEvent __instance, Exception __exception)
			{
				if (__exception != null)
				{ 
					if (PLAYER.currentSession.paused)
					{
						PLAYER.currentSession.unpause();
					}
					SCREEN_MANAGER.alerts.Enqueue("An error with a quest forced that quest to be removed. Name: " + __instance.name);
					SCREEN_MANAGER.widgetChat.AddMessage("An error with a quest forced that quest to be removed. Name: " + __instance.name, MessageTarget.Ship);
					Game1.logException(__exception, "Quest update unhandled exception");
					Community_Bug_Fixes.thrown = __instance;
				}
				return null;
			}
		}

		[HarmonyPatch(typeof(GameFile), "update")]
		public class GameFile_update
		{
			[HarmonyPostfix]
			private static void Postfix(GameFile __instance)
			{
				if (Community_Bug_Fixes.thrown != null && __instance.activeQuests != null && __instance.activeQuests.Contains(Community_Bug_Fixes.thrown))
				{
					__instance.activeQuests.Remove(Community_Bug_Fixes.thrown);
				}
				Community_Bug_Fixes.thrown = null;
			}
		}
		*/

		//fixing game crash when activating not connected console. 
		[HarmonyPatch(typeof(ConsoleAccess), "activate")]
		public class ConsoleAccess_activate
		{
			[HarmonyPrefix]
			private static void Prefix(ConsoleAccess __instance)
			{
				if(__instance.console == null)
				{
					__instance.functioning = false;
				}
			}
		}

		//fixing game setting quest marker in the wrong place if you save and load the game before getting the marker for the ship to repair in "First Life" quest resulting in quest triggers being broken
		[HarmonyPatch(typeof(Tutorial1), "test")]
		public class Tutorial1_test
		{
			[HarmonyPrefix]
			private static void Prefix(ref Vector2 ___firstShipSpot, ulong ___salvageID)
			{
				if (PLAYER.currentSession.allShips.ContainsKey(___salvageID) && ___firstShipSpot == Vector2.Zero)
				{
					___firstShipSpot = PLAYER.currentSession.allShips[___salvageID].position;
				}
			}
		}

		//fixing: if hail ship with no crew, you get stuck in blurry screen (in vanilla the statement "string name = this.representative.name;" is assigned without null check and crashing on this.representative being NULL)
		//fixing: game crash if hailing ships in arena mode
		[HarmonyPatch(typeof(HailAnimation), "setupPlayerHailSend")]
		public class HailAnimation_setupPlayerHailSend
		{
			[HarmonyPrefix]
			private static bool Prefix(Crew ___representative, List<ResponseImmediateAction> ___results)
			{
				if (PLAYER.currentSession.GetType() == typeof(BattleSessionSA) || PLAYER.currentSession.GetType() == typeof(BattleSessionTA))
				{
					return false; //supress executing the original method
				}
				if (___representative == null)
				{					
					PLAYER.currentSession.pause();
					DialogueTree dialogueTree = new DialogueTree();
					DialogueTree dialogueTree2 = new DialogueTree();
					dialogueTree2.action = new ResponseImmediateAction(() => {
						foreach (ResponseImmediateAction responseImmediateAction in ___results)
						{
							responseImmediateAction();
						}
					});
					___results.Add(new ResponseImmediateAction(() => PLAYER.currentSession.unpause()));
					var name = "One";
					dialogueTree.text = "There doesn't seem to be anyone there...";
					dialogueTree.addOption("goodbye", dialogueTree2);
					return false; //supress executing the original method
				}
				return true; //execute the original method
			}
		}

		//fixing: using logistics room on a ship allows unintended use of logistics commands on the command source ship.
		[HarmonyPatch(typeof(LogisticsScreenRev3), "doRightClick")]
		public class LogisticsScreenRev3_doRightClick
		{

			[HarmonyPrefix]
			private static void Prefix(ref string opt, Ship ___selected)
			{	
				if (opt != "" && ___selected.id == PLAYER.currentShip.id)
				{
					SCREEN_MANAGER.widgetChat.AddMessage("Invalid target. Command target ship has to be distinct from command source ship.", MessageTarget.Ship);
					opt = "";
				}
			}
		}

	}
}
