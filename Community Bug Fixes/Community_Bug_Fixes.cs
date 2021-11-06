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
using System.Collections.Concurrent;
using Module = CoOpSpRpG.Module;

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






		//fixing bug that prevented spawning Budd and Greg, preventing their quest from completion.
		//fixing bug that prevented spawning more then 1 crew on some ships.

		[HarmonyPatch(typeof(MicroCosm), "applyData")]
		public class MicroCosm_applyData
		{
			[HarmonyPrefix]
			private static bool Prefix(MicroCosm __instance, CosmMetaData data, ref float ___crystalTimer)
			{
				if (data == null)
				{
					return false;
				}
				Dictionary<ulong, ulong> dictionary = new Dictionary<ulong, ulong>();
				if (data.crew != null)
				{
					__instance.crew = new ConcurrentDictionary<byte, Crew>();
					for (int i = 0; i < data.crew.Length; i++)
					{
						Crew crew = data.crew[i];
						if (crew.state != CrewState.dead)
						{
							if (!dictionary.ContainsKey(crew.faction))
							{
								dictionary[crew.faction] = 1UL;
							}
							else
							{
								Dictionary<ulong, ulong> dictionary2 = dictionary;
								ulong num = crew.faction;
								ulong num2 = dictionary2[num];
								dictionary2[num] = num2 + 1UL;
							}
						}
						crew.currentCosm = __instance;
						crew.id = __instance.nextCrewId;
						__instance.nextCrewId += 1;
						/*
						if (crew.id <= __instance.nextCrewId)
						{
							__instance.nextCrewId = (byte)(crew.id + 1);
						}
						*/
						__instance.crew[crew.id] = crew;
						crew.goalFailed();
					}
				}
				if (data.unplacedCrew != null)
				{
					for (int j = 0; j < data.unplacedCrew.Length; j++)
					{
						Crew crew2 = data.unplacedCrew[j];
						int num3 = __instance.randomWalkableTile();
						if (num3 == -1)
						{
							break;
						}
						crew2.position = __instance.walkingLocation(__instance.tiles[num3].botRight.index);
						if (!dictionary.ContainsKey(crew2.faction))
						{
							dictionary[crew2.faction] = 1UL;
						}
						else
						{
							Dictionary<ulong, ulong> dictionary3 = dictionary;
							ulong num2 = crew2.faction;
							ulong num = dictionary3[num2];
							dictionary3[num2] = num + 1UL;
						}
						crew2.currentCosm = __instance;
						crew2.id = __instance.nextCrewId;
						__instance.nextCrewId += 1;
						/*
						if (crew2.id <= __instance.nextCrewId)
						{
							__instance.nextCrewId = (byte)(crew2.id + 1);
						}
						*/
						__instance.crew[crew2.id] = crew2;
						crew2.goalFailed();
					}
				}
				if (dictionary.Count > 0)
				{
					ulong faction = __instance.ship.id;
					ulong num4 = 0UL;
					foreach (ulong num5 in dictionary.Keys)
					{
						if (dictionary[num5] > num4)
						{
							faction = num5;
							num4 = dictionary[num5];
						}
					}
					__instance.ship.faction = faction;
				}
				__instance.manager = new CrewManager(__instance, __instance.crew);
				if (data.moduleData != null && data.moduleData.Length == __instance.modules.Count)
				{
					for (int k = 0; k < data.moduleData.Length; k++)
					{
						if (data.moduleData[k] != null)
						{
							try
							{
								__instance.modules[k].setData(data.moduleData[k]);
							}
							catch
							{
							}
						}
					}
				}
				if (data.monsters != null)
				{
					if (__instance.monsters == null)
					{
						__instance.monsters = data.monsters;
					}
					else
					{
						foreach (Monster item in data.monsters)
						{
							__instance.monsters.Add(item);
						}
					}
					foreach (Monster monster in __instance.monsters)
					{
						if (!monster.spawned || (data.ticks > 3600f && monster.speed > 0f))
						{
							monster.spawned = true;
							int num6 = __instance.randomWalkableTile();
							if (num6 == -1)
							{
								__instance.monsters = null;
								break;
							}
							monster.position = __instance.walkingLocation(__instance.tiles[num6].botRight.index);
							if (__instance.portals != null)
							{
								InsideDockSpot[] array = __instance.portals;
								for (int l = 0; l < array.Length; l++)
								{
									if (Vector2.Distance(array[l].drawLoc, monster.position) < monster.detectRange)
									{
										num6 = __instance.randomWalkableTile();
										monster.position = __instance.walkingLocation(__instance.tiles[num6].botRight.index);
										break;
									}
								}
							}
							if (__instance.portals != null)
							{
								InsideDockSpot[] array = __instance.portals;
								for (int l = 0; l < array.Length; l++)
								{
									if (Vector2.Distance(array[l].drawLoc, monster.position) < monster.detectRange)
									{
										num6 = __instance.randomWalkableTile();
										monster.position = __instance.walkingLocation(__instance.tiles[num6].botRight.index);
										break;
									}
								}
							}
							monster.spawnLocation = monster.position;
						}
					}
				}
				if (data.crystals != null)
				{
					__instance.crystals = data.crystals;
				}
				___crystalTimer = data.ticks;
				if (data.air != null)
				{
					for (int m = 0; m < __instance.tiles.Length; m++)
					{
						__instance.tiles[m].air = data.air[m];
					}
				}
				if (data.reactorHeat != null)
				{
					int num7 = 0;
					foreach (CoOpSpRpG.Module module in __instance.modules)
					{
						if (module.type == ModuleType.reactor)
						{
							(module as Reactor).heat = data.reactorHeat[num7];
							num7++;
						}
					}
				}
				float num8 = 0.19999701f;
				for (float num9 = 0f; num9 < data.ticks; num9 += num8)
				{
					__instance.dissopateHeat(0.2f);
				}
				if (data.missiles != null)
				{
					int num10 = 0;
					foreach (CoOpSpRpG.Module module2 in __instance.modules)
					{
						if (module2.type == ModuleType.launcher)
						{
							if (data.missiles[num10] != null)
							{
								(module2 as Launcher).tube.Enqueue(data.missiles[num10]);
							}
							num10++;
						}
						if (module2.type == ModuleType.missile_magazine)
						{
							(module2 as MissileMagazine).missile = data.missiles[num10];
							num10++;
						}
					}
				}
				__instance.missileType = data.missileType;
				if (data.reload)
				{
					__instance.rearm = true;
				}
				__instance.flickerOverride = data.flickering;
				data.reload = false;
				if (data.addCrystals > 0 && PLAYER.currentGame != null)
				{
					List<int> list = new List<int>();
					for (int n = 0; n < __instance.bot.Length; n++)
					{
						if (__instance.bot[n].R == 33 && __instance.bot[n].G == 19 && __instance.bot[n].B == 11)
						{
							list.Add(n);
						}
					}
					int num11 = Math.Min(list.Count, data.addCrystals);
					for (int num12 = 0; num12 < num11; num12++)
					{
						int num13 = list[RANDOM.Next(list.Count)];
						Vector2 spot = new Vector2((float)(16 * (num13 % __instance.width)), (float)(16 * (num13 / __instance.width)));
						__instance.crystals.Add(new CrystalMonster(spot));
					}
				}
				int num14 = 0;
				if (data.storage != null && data.storage.Length != 0)
				{
					List<InventoryItem> list2 = new List<InventoryItem>();
					if (data.loot != null)
					{
						foreach (InventoryItem item2 in data.loot)
						{
							list2.Add(item2);
						}
					}
					foreach (Module module3 in __instance.modules)
					{
						if (module3.type == ModuleType.cargo_bay)
						{
							CargoBay cargoBay = module3 as CargoBay;
							if (data.storage[num14] != null && data.storage[num14].inventory != null)
							{
								if (cargoBay.functioning)
								{
									cargoBay.storage = data.storage[num14];
								}
								else
								{
									foreach (InventoryItem inventoryItem in data.storage[num14].inventory)
									{
										if (inventoryItem != null)
										{
											list2.Add(inventoryItem);
										}
									}
								}
							}
							num14++;
							if (data.storage != null && num14 >= data.storage.Length)
							{
								break;
							}
						}
					}
					if (list2.Count > 0)
					{
						List<CargoBay> list3 = new List<CargoBay>();
						foreach (Module module4 in __instance.modules)
						{
							if (module4.type == ModuleType.cargo_bay)
							{
								list3.Add(module4 as CargoBay);
							}
						}
						RANDOM.shuffle<CargoBay>(ref list3);
						foreach (CargoBay cargoBay2 in list3)
						{
							if (cargoBay2.functioning && cargoBay2.storage != null)
							{
								while (list2.Count > 0)
								{
									InventoryItem item3 = list2.First<InventoryItem>();
									if (!cargoBay2.storage.placeInFirstSlot(item3))
									{
										break;
									}
									list2.Remove(item3);
								}
							}
						}
						list2.Clear();
					}
				}
				if (data.systemsData != null)
				{
					int num15 = 0;
					foreach (TacticalSystem tacticalSystem in __instance.systems)
					{
						TacticalSystem tacticalSystem2 = tacticalSystem as TacticalSystem;
						if (data.systemsData[num15] != null)
						{
							tacticalSystem2.readData(data.systemsData[num15]);
						}
						num15++;
					}
				}
				if (data.routeLists != null)
				{
					foreach (EngineeringRoom engineeringRoom in __instance.engineeringRooms)
					{
						for (int num16 = 0; num16 < engineeringRoom.routeCount; num16++)
						{
							try
							{
								if (engineeringRoom.pointLists == null)
								{
									engineeringRoom.pointLists = new List<List<Point>[]>();
								}
								if (data.routeLists[0] == null)
								{
									engineeringRoom.pointLists.Add(new List<Point>[3]);
								}
								else
								{
									engineeringRoom.pointLists.Add(data.routeLists[0]);
								}
								data.routeLists.RemoveAt(0);
								engineeringRoom.configure();
							}
							catch
							{
								break;
							}
						}
					}
				}
				num14 = 0;
				if (data.artifacts != null)
				{
					foreach (Module module5 in __instance.modules)
					{
						if (module5.type == ModuleType.artifact_activator)
						{
							ArtifactActivator artifactActivator = module5 as ArtifactActivator;
							if (data.artifacts[num14] != null && data.artifacts[num14].inventory != null && artifactActivator.functioning)
							{
								artifactActivator.storage = data.artifacts[num14];
							}
							num14++;
							if (num14 >= data.artifacts.Length)
							{
								break;
							}
						}
					}
				}
				if (data.goo != null && data.goo.Length == __instance.modules.Count)
				{
					for (int num17 = 0; num17 < __instance.modules.Count; num17++)
					{
						__instance.modules[num17].goo = data.goo[num17];
					}
				}
				return false; //instruction for harmony to supress executing the original method
			}
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



		/*
		[HarmonyPatch(typeof(PirateFactionRev2), "updateFlotillas")]
		public class PirateFactionRev2_updateFlotillas
		{
			[HarmonyPrefix]
			private static bool Prefix(PirateFactionRev2 __instance, Dictionary<ulong, List<Point>> ___specialFlotillas, List<ulong> ___flotillas, List<Point> ___redZone, List<Point> ___yellowZone)
			{
				foreach (ulong key in ___specialFlotillas.Keys)
				{
					if (!__instance.ships.ContainsKey(key))
					{
						___specialFlotillas.Remove(key);
						break;
					}
					if (__instance.ships[key].isAtDestination)
					{
						List<Point> list = ___specialFlotillas[key];
						Point g = list[RANDOM.Next(list.Count)];
						__instance.ships[key].patrolToGridRandom(g);
					}
				}
				foreach (ulong num in ___flotillas)
				{
					if (__instance.ships.ContainsKey(num))
					{
						if (__instance.ships[num].isAtDestination)
						{
							Point grid = __instance.ships[num].grid;
							Point g2 = grid;
							if (RANDOM.Next(5) == 0)
							{
								int num2 = RANDOM.Next(2);
								if (num2 != 0)
								{
									if (num2 == 1)
									{
										g2 = ___yellowZone[RANDOM.Next(___yellowZone.Count)];
									}
								}
								else
								{
									g2 = ___redZone[RANDOM.Next(___redZone.Count)];
								}
							}
							else
							{
								if (___redZone.Contains(grid))
								{
									g2 = ___redZone[RANDOM.Next(___redZone.Count)];
								}
								if (___yellowZone.Contains(grid))
								{
									g2 = ___yellowZone[RANDOM.Next(___yellowZone.Count)];
								}
								if (___redZone.Contains(grid))
								{
									g2 = ___redZone[RANDOM.Next(___redZone.Count)];
								}
								g2.X -= 8;
								g2.X += RANDOM.Next(17);
								g2.Y -= 8;
								g2.Y += RANDOM.Next(17);
							}
							__instance.ships[num].patrolToGridRandom(g2);
						}
					}
					else
					{
						___flotillas.Remove(num);
						break;
					}
				}
				return false; //instruction for harmony to supress executing the original method
			}
		}

		[HarmonyPatch(typeof(FreelancerFactionRev2), "updateFlotillas")]
		public class FreelancerFactionRev2_updateFlotillas
		{
			[HarmonyPrefix]
			private static bool Prefix(FreelancerFactionRev2 __instance, Dictionary<ulong, List<Point>> ___specialFlotillas, List<ulong> ___flotillas, List<Point> ___redZone, List<Point> ___yellowZone, List<Point> ___greyZone)
			{
				foreach (ulong key in ___specialFlotillas.Keys)
				{
					if (!__instance.ships.ContainsKey(key))
					{
						___specialFlotillas.Remove(key);
						break;
					}
					else if (__instance.ships[key].isAtDestination)
					{
						List<Point> list = ___specialFlotillas[key];
						Point g = list[RANDOM.Next(list.Count)];
						__instance.ships[key].patrolToGridRandom(g);
					}
				}
				foreach (ulong num in ___flotillas)
				{
					if (__instance.ships.ContainsKey(num))
					{
						if (__instance.ships[num].isAtDestination)
						{
							Point grid = __instance.ships[num].grid;
							Point g2 = grid;
							if (RANDOM.Next(5) == 0)
							{
								int num2 = RANDOM.Next(2);
								if (num2 != 0)
								{
									if (num2 == 1)
									{
										g2 = ___yellowZone[RANDOM.Next(___yellowZone.Count)];
									}
								}
								else
								{
									g2 = ___greyZone[RANDOM.Next(___greyZone.Count)];
								}
							}
							else
							{
								if (___greyZone.Contains(grid))
								{
									g2 = ___greyZone[RANDOM.Next(___greyZone.Count)];
								}
								if (___yellowZone.Contains(grid))
								{
									g2 = ___yellowZone[RANDOM.Next(___yellowZone.Count)];
								}
								if (___redZone.Contains(grid))
								{
									g2 = ___redZone[RANDOM.Next(___redZone.Count)];
								}
								g2.X -= 8;
								g2.X += RANDOM.Next(17);
								g2.Y -= 8;
								g2.Y += RANDOM.Next(17);
							}
							__instance.ships[num].patrolToGridRandom(g2);
						}
					}
					else
					{
						___flotillas.Remove(num);
						break;
					}
				}
				return false; //instruction for harmony to supress executing the original method
			}
		}

		[HarmonyPatch(typeof(SSCFactionRev2), "updateFlotillas")]
		public class SSCFactionRev2_updateFlotillas
		{
			[HarmonyPrefix]
			private static bool Prefix(SSCFactionRev2 __instance, Dictionary<ulong, List<Point>> ___specialFlotillas, List<ulong> ___flotillas, List<Point> ___asteroidSpots, List<Point> ___spawnSpots)
			{
				foreach (ulong key in ___specialFlotillas.Keys)
				{
					if (!__instance.ships.ContainsKey(key))
					{
						___specialFlotillas.Remove(key);
						break;
					}
					else if (__instance.ships[key].isAtDestination)
					{
						List<Point> list = ___specialFlotillas[key];
						Point g = list[RANDOM.Next(list.Count)];
						__instance.ships[key].patrolToGridRandom(g);
					}
				}
				foreach (ulong num in ___flotillas)
				{
					if (__instance.ships.ContainsKey(num))
					{
						if (__instance.ships[num].isAtDestination)
						{
							Point grid = __instance.ships[num].grid;
							Point g2 = grid;
							if (RANDOM.Next(5) == 0)
							{
								int num2 = RANDOM.Next(2);
								if (num2 != 0)
								{
									if (num2 == 1)
									{
										g2 = ___asteroidSpots[RANDOM.Next(___asteroidSpots.Count)];
									}
								}
								else
								{
									g2 = ___spawnSpots[RANDOM.Next(___spawnSpots.Count)];
								}
							}
							else
							{
								if (___spawnSpots.Contains(grid))
								{
									g2 = ___spawnSpots[RANDOM.Next(___spawnSpots.Count)];
								}
								if (___asteroidSpots.Contains(grid))
								{
									g2 = ___asteroidSpots[RANDOM.Next(___asteroidSpots.Count)];
								}
								g2.X -= 8;
								g2.X += RANDOM.Next(17);
								g2.Y -= 8;
								g2.Y += RANDOM.Next(17);
							}
							__instance.ships[num].patrolToGridRandom(g2);
						}
					}
					else
					{
						___flotillas.Remove(num);
						break;
					}
				}
				return false; //instruction for harmony to supress executing the original method
			}
		}
		*/



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
