using System;
using System.Collections.Generic;
using HarmonyLib;
using CoOpSpRpG;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WTFModLoader;
using WTFModLoader.Manager;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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

		//fixing: Ships at your homebase despawn if you have been absent for a while (now ships no longer despawn if they are docked to your homebase)
		[HarmonyPatch(typeof(Ship), "mandatoryUpdate")]
		public class Ship_mandatoryUpdate
		{
			[HarmonyPostfix]
			private static void Postfix(Ship __instance, ref float ___deathTimer)
			{
				if (PLAYER.currentGame != null)
				{ 
					if (__instance.dockedAt != null && __instance.dockedAt.id == PLAYER.currentGame.homeBaseId && __instance.fadeOutTimer > 0f)
					{
						___deathTimer = 0f;
						__instance.fadeOutTimer = -10f;
					}
				}
			}
		}

		//fixing: Unable to recruit Vaal after conversation with her. (preventing the bug from happening)
		[HarmonyPatch(typeof(ValAgent), "finishConvo")]
		public class ValAgent_finishConvo
		{
			[HarmonyPostfix]
			private static void Postfix(ValAgent __instance, bool ___adopted)
			{
				if(___adopted == true && __instance.canJoin == false)
				{
					if (SCREEN_MANAGER.dialogue.removeMe)
					{
						__instance.canJoin = true;
						PLAYER.currentGame.agentTracker.adoptAgent(__instance.name);
						bool found = false;
						if (PLAYER.currentGame != null && PLAYER.currentGame.activeQuests != null)
						{
							foreach (TriggerEvent triggerEvent in PLAYER.currentGame.activeQuests)
							{
								if (triggerEvent.name == "find_crew")
								{
									triggerEvent.stage += 1U;
									found = true;
								}
							}
						}
						if (CHARACTER_DATA.maxCrew == 0)
						{
							PLAYER.currentGame.activeQuests.Add(new CloningReminder(found));
						}
					}
				}
			}
		}

		//fixing: Unable to recruit Vaal after conversation with her. (fixing already bugged saves)
		[HarmonyPatch(typeof(AgentTracker), "getBarAgents")] 
		public class AgentTracker_getBarAgents
		{

			[HarmonyPrefix]
			private static void Prefix(AgentTracker __instance, ref List<BarAgentDrawer> __result, ulong stationID, Point grid)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
				foreach (NPCAgent npcagent in __instance.allAgents)
				{
					if (npcagent.name == "Vaal")
					{
						var field = typeof(ValAgent).GetField("adopted", flags);
						var adopted = (bool)field.GetValue(npcagent);
						if (!__instance.unlockedFriends.Contains(npcagent) && adopted == true)
						{
							npcagent.canJoin = true;
							__instance.adoptAgent(npcagent.name);
							bool found = false;
							if (PLAYER.currentGame != null && PLAYER.currentGame.activeQuests != null)
							{
								foreach (TriggerEvent triggerEvent in PLAYER.currentGame.activeQuests)
								{
									if (triggerEvent.name == "find_crew")
									{
										triggerEvent.stage += 1U;
										found = true;
									}
								}
							}
							if (CHARACTER_DATA.maxCrew == 0)
							{
								PLAYER.currentGame.activeQuests.Add(new CloningReminder(found));
							}
						}
					}
				}
			}
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
		/*
		 * obsolete  (fixed in 0.9.0.07)
		 * 
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
		*/

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

		/*  TODO in BattleSessionSP.localUpdate()
		if (ship18 == PLAYER.currentShip)
				{
					if (ship18.grid != this.grid && PLAYER.avatar != null && PLAYER.avatar.state != CrewState.dead)
					{
						this.allShips.Remove(ship18.id);																	//removing from collection while iterating
						PLAYER.currentSession = PLAYER.currentWorld.getSession(ship18.grid);
						PLAYER.currentSession.addLocalShip(ship18, SessionEntry.flyin);
						this.shareNeighbors(PLAYER.currentSession);
						this.deportVisitors();
						if (Math.Abs(ship18.grid.X - this.grid.X) < 2 && Math.Abs(ship18.grid.Y - this.grid.Y) < 2)
						{
							Vector2 value4 = Vector2.Zero;
							for (int j = 0; j< this.neighbors.Length; j++)
							{
								if (this.neighbors[j] == PLAYER.currentSession)
								{
			value4 = BattleSession.neighborOffsets[j];
			break;
		}
		}
		Vector2 value5 = PLAYER.currentShip.position;
		value5 += value4;
							BattleSessionSP battleSessionSP = PLAYER.currentSession as BattleSessionSP;
							foreach (Ship ship27 in this.allShips.Values)													//iterating over the same collection again, probably the source of exception: Crash caused by: core game update unhandled exception Collection was modified; enumeration operation may not execute.
							{
								if (ship27 != ship18 && ship27.bBox.Intersects(PLAYER.viewableArea) && Vector2.Distance(value5, ship27.position) < 10000f)
								{
									this.sessionVisitors.Remove(ship27);
									battleSessionSP.allShips[ship27.id] = ship27;
									ship27.position -= value4;
									if (Math.Abs(ship27.position.X) > 100000f || Math.Abs(ship27.position.Y) > 100000f)
									{
										battleSessionSP.sessionVisitors.Add(ship27);
									}
									ship27.grid = battleSessionSP.grid;
									ship27.findHitbox();
								}
							}


		*/

		//hotfix for missing stations bug in 9.0.0.10 (fixed in 9.0.0.11)
		/* 
		[HarmonyPatch(typeof(WorldActor), "getStation", new Type[] {})]
		public class WorldActor_getStation
		{
			[HarmonyPostfix]
			private static void Postfix(WorldActor __instance, ref Station __result)
			{
				if (__instance.type == ActorType.station)
				{
					Station station = null;
					if (__instance.top != null)
					{
						station = new Station(__instance.top, __instance.bot, __instance.emit, __instance.spec, __instance.bump, __instance.width, __instance.height, __instance.collision);
						station.data = __instance.data;
						if (__instance.turrets != null)
						{
							station.turrets = __instance.turrets;
							foreach (Turret turret in station.turrets)
							{
								if (turret != null)
								{
									turret.ship = station;
								}
							}
						}
						else
						{
							station.turrets = new Turret[0];
						}
						uint num = 0U;
						float num2 = 0f;
						float num3 = 0f;
						uint num4 = 0U;
						uint num5 = 0U;
						for (int j = 0; j < __instance.top.Length; j++)
						{
							if (__instance.bot[j].A > 0)
							{
								num += 1U;
								num2 += num4;
								num3 += num5;
							}
							if (__instance.top[j].A > 0)
							{
								num += 1U;
								num2 += num4;
								num3 += num5;
							}
							num4 += 1U;
							if ((ulong)num4 == (ulong)((long)__instance.width))
							{
								num4 = 0U;
								num5 += 1U;
							}
						}
						if (num > 0U)
						{
							__instance.centerOfMass.X = num2 / num;
							__instance.centerOfMass.Y = num3 / num;
						}
						else
						{
							__instance.centerOfMass.X = 0f;
							__instance.centerOfMass.Y = 0f;
						}
						station.setID(__instance.id);
						station.ownershipHistory = __instance.ownershipHistory;
						station.setFaction(__instance.faction);
						station.hackingAvailable = __instance.hackingAvailable;
						station.findAirlocks();
						station.artOrigin = __instance.centerOfMass;
					}
					if (station != null)
					{
						station.position = __instance.position;
						station.grid = __instance.grid;
						station.rotationAngle = __instance.rotation;
						station.docksToPerform = __instance.dockedShips;
						station.toggleDocking(__instance.dockingActive);
						CosmMetaData data = __instance.data;
					}
					__result = station;
					return;
				}
				__result = null;
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
		//fixing: unable to use logistics room to stash resources from homebase cargobays to ship building resources pool. (now you can use "Unload cargo" on your homebase)
		[HarmonyPatch(typeof(LogisticsScreenRev3), "doRightClick")]
		public class LogisticsScreenRev3_doRightClick
		{

			[HarmonyPrefix]
			private static void Prefix(LogisticsScreenRev3 __instance, ref string opt, Ship ___selected)
			{	
				if (opt != "" && ___selected.id == PLAYER.currentShip.id && ___selected.id != PLAYER.currentGame.homeBaseId)
				{
					SCREEN_MANAGER.widgetChat.AddMessage("Invalid target. Command target ship has to be distinct from command source ship.", MessageTarget.Ship);
					opt = "";
				}
				if (opt == "Unload cargo" && ___selected.id == PLAYER.currentGame.homeBaseId && PLAYER.currentSession.GetType() == typeof(BattleSessionSP))
				{
					var shipStores = CHARACTER_DATA.getCargoTabs();
					List<string> cargoNames = CHARACTER_DATA.getCargoNames();
					BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
					if (shipStores.Count > 0)
					{
						for (int i = 0; i < shipStores.Count; i++)
						{
							var openStorage = shipStores[i];
							
							string[] storageTabNames;
							if (cargoNames.Count != shipStores.Count)
							{
								storageTabNames = new string[shipStores.Count];
								for (int j = 0; j < shipStores.Count; j++)
								{
									storageTabNames[j] = "Tab " + j.ToString();
								}
							}
							else
							{
								storageTabNames = cargoNames.ToArray();
							}
							var args = new object[] { openStorage };
							typeof(LogisticsScreenRev3).GetMethod("stashResources", flags, null, new Type[] { typeof(Storage) }, null).Invoke(__instance, args);
							CHARACTER_DATA.storeCargoTab(i, storageTabNames[i], args[0] as Storage);
						}
						opt = "";
					}			
				}

				// scraping a ship with cargo get's the cargo deleted (now it will be placed as cargo pods in space instead)
				if (opt == "Scrap" && PLAYER.currentSession.GetType() == typeof(BattleSessionSP))
				{
					var ship = ___selected;
					MicroCosm cosm = PROCESS_REGISTER.getCosm(ship);
					if (cosm.cargoBays != null && cosm.cargoBays.Count > 0)
					{
						for (int j = 0; j < cosm.cargoBays.Count; j++)
						{
							if (cosm.cargoBays[j].storage != null)
							{
								if (cosm.cargoBays[j].storage.inventory == null)
								{
									return;
								}
								for (int i = 0; i < cosm.cargoBays[j].storage.inventory.Length; i++)
								{
									if (cosm.cargoBays[j].storage.inventory[i] != null)
									{
										while (cosm.cargoBays[j].storage.inventory[i] != null)
										{
											InventoryItem item = cosm.cargoBays[j].storage.getItem(i);
											if (item != null)
											{
												CargoPod cargoPod = new CargoPod(item, ship.position);
												CargoPod cargoPod2 = cargoPod;
												cargoPod2.position.X = cargoPod2.position.X + ((float)(RANDOM.NextDouble() * 100.0) - 50f);
												CargoPod cargoPod3 = cargoPod;
												cargoPod3.position.Y = cargoPod3.position.Y + ((float)(RANDOM.NextDouble() * 100.0) - 50f);
												PLAYER.currentSession.cargo.Add(cargoPod);
												PLAYER.currentSession.cargoDetection(cargoPod.position, true);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		//fixing: unable to use logistics room to stash resources from homebase cargobays to ship building resources pool. (now you can use "Unload cargo" on your homebase)
		[HarmonyPatch(typeof(LogisticsScreenRev3), "updateInput")]
		public class LogisticsScreenRev3_updateInput
		{
			[HarmonyPrefix]
			private static void Prefix(ref MouseState __state, MouseState ___oldMouse)
			{
				__state = ___oldMouse;
			}


			[HarmonyPostfix]
			private static void Postfix(LogisticsScreenRev3 __instance, MouseState __state, bool ___drawSpawn, bool ___pause, ref DropDown ___activeMenu, ref Ship ___hover, ref Ship ___selected, Vector2 ___mousePos)
			{
				if (!___drawSpawn && !___pause && Mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released && __state.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && ___activeMenu == null)
				{
					if (___hover == null)
					{
						Rectangle clickPos = new Rectangle((int)___mousePos.X, (int)___mousePos.Y, 1, 1);
						if (clickPos.Intersects(PLAYER.currentShip.bBox) && PLAYER.currentShip.id == PLAYER.currentGame.homeBaseId)
						{
							___hover = PLAYER.currentShip;
						}						
						if (PLAYER.currentShip == ___hover)
						{
							___activeMenu = new DropDown(___mousePos);
							if ((___hover.cosm != null && ___hover.cosm.cargoBays.Count > 0) || (___hover.data != null && ___hover.data.storage != null))
							{
								___activeMenu.addOption("Unload cargo", null);
							}
							___selected = ___hover;
						}
					}
				}

			}
		}

		//fixing: passing through airlock to the docked ship while pressing movement keys will instatly return you to the ship you have left if both ships have their airlocks on the same axis.
		[HarmonyPatch(typeof(ShipNavigationRev3), "updateInput")]
		public class ShipNavigationRev3_updateInput
		{
			[HarmonyPrefix]
			private static void Prefix(ref KeyboardState __state, KeyboardState ___oldState)
			{
				__state = ___oldState;
			}

			[HarmonyPostfix]
			private static void Postfix(ShipNavigationRev3 __instance, KeyboardState __state)
			{
				KeyboardState newstate = Keyboard.GetState();
				if(PLAYER.avatar != null &&
				__state.IsKeyDown(CONFIG.keyBindings[0].bind) && newstate.IsKeyUp(CONFIG.keyBindings[0].bind) 
				|| __state.IsKeyDown(CONFIG.keyBindings[1].bind) && newstate.IsKeyUp(CONFIG.keyBindings[1].bind) 
				|| __state.IsKeyDown(CONFIG.keyBindings[2].bind) && newstate.IsKeyUp(CONFIG.keyBindings[2].bind) 
				|| __state.IsKeyDown(CONFIG.keyBindings[3].bind) && newstate.IsKeyUp(CONFIG.keyBindings[3].bind)
				)
				{
					if(PLAYER.avatar.shallNotPass() && PLAYER.avatar.shuffledBinds())
					{
						var temp = CONFIG.keyBindings[0].bind;
						CONFIG.keyBindings[0].bind = CONFIG.keyBindings[3].bind;
						CONFIG.keyBindings[3].bind = temp;
						temp = CONFIG.keyBindings[1].bind;
						CONFIG.keyBindings[1].bind = CONFIG.keyBindings[2].bind;
						CONFIG.keyBindings[2].bind = temp;
						PLAYER.avatar.shuffledBinds(false);
					}
					PLAYER.avatar.shallNotPass(false);
				}
			}
		}
		//fixing: passing through airlock to the docked ship while pressing movement keys will instatly return you to the ship you have left if both ships have their airlocks on the same axis.
		[HarmonyPatch(typeof(Airlock), "tryUse")]
		public class Airlock_tryUse
		{
			[HarmonyPrefix]
			private static bool Prefix(Airlock __instance, Crew c)
			{
				if (__instance.spot != null && c == PLAYER.avatar)
				{

					if(PLAYER.avatar.shallNotPass())
					{
						return false;
                    }				
					PLAYER.avatar.shallNotPass(true);
					PLAYER.avatar.passDirection(__instance.connectDirection);
				}
				return true;
			}
		}

		//fixing: passing through airlock to the docked ship while pressing movement keys will instatly return you to the ship you have left if both ships have their airlocks on the same axis.
		[HarmonyPatch(typeof(DockSpot), "receiveCrew")]
		public class DockSpot_receiveCrew
		{
			[HarmonyPostfix]
			private static void Postfix(DockSpot __instance, Crew c)
			{
				if (c == PLAYER.avatar && PLAYER.avatar.shallNotPass())
				{
					if (PLAYER.avatar.passDirection() == __instance.airlock.connectDirection)
					{
						var temp = CONFIG.keyBindings[0].bind;
						CONFIG.keyBindings[0].bind = CONFIG.keyBindings[3].bind;
						CONFIG.keyBindings[3].bind = temp;
						temp = CONFIG.keyBindings[1].bind;
						CONFIG.keyBindings[1].bind = CONFIG.keyBindings[2].bind;
						CONFIG.keyBindings[2].bind = temp;
						PLAYER.avatar.shuffledBinds(true);
					}
				}
			}
		}
		//fixing: passing through airlock to the docked ship while pressing movement keys will instatly return you to the ship you have left if both ships have their airlocks on the same axis.
		[HarmonyPatch(typeof(CrewManager), "murder")]
		public class CrewManager_murder
		{
			[HarmonyPostfix]
			private static void Postfix(List<byte> ___removal, MicroCosm ___currentCosm)
			{
				if(PLAYER.avatar != null && PLAYER.avatar.shallNotPass() && ___removal.Contains(PLAYER.avatar.id) && !___currentCosm.crew.ContainsKey(PLAYER.avatar.id))
				{ 
					___currentCosm.crew.TryAdd(PLAYER.avatar.id, PLAYER.avatar);
				}
			}
		}

	}
	//fixing: passing through airlock to the docked ship while pressing movement keys will instatly return you to the ship you have left if both ships have their airlocks on the same axis.
	public static class CrewExtensions
	{
		static readonly ConditionalWeakTable<Crew, ShallNotPassObject> shallnotpass = new ConditionalWeakTable<Crew, ShallNotPassObject>();
		public static bool shallNotPass(this Crew crew) { return shallnotpass.GetOrCreateValue(crew).Value; }

		public static void shallNotPass(this Crew crew, bool setshallnotpass) { shallnotpass.GetOrCreateValue(crew).Value = setshallnotpass; }

		class ShallNotPassObject
		{
			public bool Value = new bool();
		}

		static readonly ConditionalWeakTable<Crew, PassDirectionObject> passdirection = new ConditionalWeakTable<Crew, PassDirectionObject>();
		public static ConnectDirection passDirection(this Crew crew) { return passdirection.GetOrCreateValue(crew).Value; }

		public static void passDirection(this Crew crew, ConnectDirection newpassdirection) { passdirection.GetOrCreateValue(crew).Value = newpassdirection; }

		class PassDirectionObject
		{
			public ConnectDirection Value = new ConnectDirection();
		}

		static readonly ConditionalWeakTable<Crew, ShuffledBindsObject> shuffledbinds = new ConditionalWeakTable<Crew, ShuffledBindsObject>();
		public static bool shuffledBinds(this Crew crew) { return shuffledbinds.GetOrCreateValue(crew).Value; }

		public static void shuffledBinds(this Crew crew, bool setshuffledbinds) { shuffledbinds.GetOrCreateValue(crew).Value = setshuffledbinds; }

		class ShuffledBindsObject
		{
			public bool Value = new bool();
		}
	}


}
