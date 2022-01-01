using System;
using System.Collections.Generic;
using HarmonyLib;
using CoOpSpRpG;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WTFModLoader;
using WTFModLoader.Manager;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.IO;
using System.Data.SQLite;
using System.Data;
using Module = CoOpSpRpG.Module;
using System.Collections.Concurrent;
using Console = CoOpSpRpG.Console;
using System.Linq;

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


		//fixing: several conditions which resulted in duplicating crew.
		[HarmonyPatch(typeof(MicroCosm), "updateCrew")]
		public class MicroCosm_updateCrew
		{
			[HarmonyPrefix]
			private static void Prefix(MicroCosm __instance)
			{
				foreach (Crew crew in __instance.crew.Values)
				{
					if (!crew.isPlayer && crew.currentCosm == __instance && crew.state == CrewState.dead && __instance.crew.Values.Where((item) => item.name == crew.name && item.state != CrewState.dead).Any())
					{
						Crew crew2;
						__instance.crew.TryRemove(crew.id, out crew2);
					}
					if (!crew.isPlayer && crew.currentCosm == __instance && crew.state != CrewState.dead && __instance.crew.Values.Where((item) => item != crew && item.faction == 2UL && crew.faction == 2UL && item.name == crew.name && item.state != CrewState.dead).Any())
					{
						Crew crew2;
						__instance.crew.TryRemove(crew.id, out crew2);
					}
				}
			}
		}
		

		//fixing: 
		/*
		[HarmonyPatch(typeof(CrewTeamWidget), "update")]
		public class CrewTeamWidget_update
		{
			[HarmonyPrefix]
			private static bool Prefix(CrewTeamWidget __instance, ConcurrentQueue<Object> ___reports)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
				while (___reports.TryDequeue(out var result))
				{

					if (((IEnumerable<Crew>)__instance.crew).Contains<Crew>((Crew)typeof(CrewTeamWidget).Assembly.GetType("CoOpSpRpG.StatusReport").GetField("crew", flags).GetValue(result)))
					{
						//__instance.readReport(result);
						var args = new object[] { result };
						typeof(LogisticsScreenRev3).GetMethod("readReport", flags, null, new Type[] { typeof(CrewTeamWidget).Assembly.GetType("CoOpSpRpG.StatusReport") }, null).Invoke(__instance, args);			
					}
					else
					{
						bool flag = false;
						for (int index = 0; index < __instance.crew.Length; ++index)
						{
							Crew crew = (Crew)typeof(CrewTeamWidget).Assembly.GetType("CoOpSpRpG.StatusReport").GetField("crew", flags).GetValue(result);
							if (__instance.crew[index] == null && !__instance.names.Contains(crew.name))
							{
								flag = true;
								__instance.crew[index] = crew;
								break;
							}
						}
						if (flag)
						{
							//__instance.readReport(result);
							var args = new object[] { result };
							typeof(LogisticsScreenRev3).GetMethod("readReport", flags, null, new Type[] { typeof(CrewTeamWidget).Assembly.GetType("CoOpSpRpG.StatusReport") }, null).Invoke(__instance, args);
						}					
					}
				}
				foreach (Crew dead in __instance.crew)
				{
					if (dead != null && dead.state == CrewState.dead)
						__instance.handleDeath(dead);
				}
				return false;
			}
		}
		*/

		//fixing: crystal seed tooltip is not updating if you select a crystal with 0% seed value after a crystal with more than 0% seed value
		[HarmonyPatch(typeof(Dig), "aim")]
		public class Dig_aim
		{
			[HarmonyPostfix]
			private static void Postfix(MicroCosm cosm, Vector2 target, TipStatSmall ___stat3)
			{
				Rectangle value = new Rectangle((int)target.X - 1, (int)target.Y - 1, 2, 2);
				foreach (CrystalMonster crystalMonster in cosm.crystals)
				{
					if (crystalMonster.bbox.Intersects(value))
					{				
						float num3 = (float)(crystalMonster.seedE + crystalMonster.seedM);
						if (num3 <= 0f)
						{
							___stat3.updateStat("-");
						}
						break;
					}
				}
			}
		}

		//fixing: planted crystals are no longer reproducing after a save/reload
		//fixing: if you save and reload the game after planting a crystal and before it grows a root it will no longer grow
		[HarmonyPatch(typeof(CrystalMonster))]
		[HarmonyPatch(MethodType.Constructor)]
		[HarmonyPatch(new Type[] { typeof(BinaryReader), typeof(int) })]
		public class CrystalMonster_CrystalMonster
		{

			[HarmonyPostfix]
			private static void Postfix(CrystalMonster __instance)
			{
				int activeroots = 0;
				for (int i = 0; i < __instance.genome.tiles.Length; i++)
				{
					for (int j = 0; j < __instance.genome.tiles.Length; j++) // in vanilla "spreadCostE", "spreadCostm" stats are not lodead  with game reload an will remain 0
					{
						CrystalGene crystalGene = __instance.genome.tiles[i][j];
						if (crystalGene != null)
						{
							if (crystalGene.crystalType == CrystalType.growth)
							{
								__instance.spreadCostE += crystalGene.cost;
								__instance.spreadCostM += crystalGene.cost;
							}
							if (crystalGene.crystalType == CrystalType.shell)
							{
								__instance.spreadCostE += 90;
								__instance.spreadCostM += 90;
							}
							if (crystalGene.crystalType == CrystalType.battery)
							{
								__instance.spreadCostM += 10;
								__instance.spreadCostE += 90;
							}
							if (crystalGene.crystalType == CrystalType.root && crystalGene.active == true)
							{
								activeroots++;
							}
						}
					}
				}
				if (activeroots == 0) 
				{
					for (int i = 0; i < __instance.genome.tiles.Length; i++)
					{
						for (int j = 0; j < __instance.genome.tiles.Length; j++)
						{
							CrystalGene crystalGene = __instance.genome.tiles[i][j];
							if (crystalGene != null)
							{
								 if(crystalGene.crystalType != CrystalType.shell)
								 { 
									__instance.minerals -= crystalGene.built;
								 }
								 else
								 {
									if (crystalGene.built != 0)
										__instance.minerals += 80; // in vanilla "minerals" stat is not saved/lodead  with game reload
								}
							}
						}
					}
					if (__instance.minerals < 0)
					{
						__instance.minerals = 0;
					}
				}
			}
		}

		//fixing: crew is firing on target out of range of their weapon and not trying to get in range
		[HarmonyPatch(typeof(Crew), "testLOS")] 
		public class Crew_testLOS
		{
			[HarmonyPostfix]
			private static void Postfix(Crew __instance, ref bool __result, Vector2 target, MicroCosm cosm)
			{
				if (__instance.heldItem != null && __instance.heldItem.GetType() == typeof(Gun))
				{
					float num = (__instance.heldItem as Gun).range;
					float num2 = Vector2.Distance(__instance.position, target);
					if (num2 > num)
					{
						__result = false;
					}
				}
			}
		}

		//fixing: followers are not equiping their weapon and attacking a monster if they see one and have some other tool equiped.
		//fixing: a rare crash while crew firing their weapon
		[HarmonyPatch(typeof(Crew), "attack")]
		public class Crew_attack
		{

			[HarmonyPrefix]
			private static bool Prefix(Crew __instance, float elapsed,ref float ___floatRegister, ref Vector2 ___strafeSpot, ref bool ___targetLOS)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;			
				{
					
					if (__instance.goal.GetType() == typeof(Crew))
					{
						Crew crew = __instance.goal as Crew;
						if (crew.state == CrewState.dead || crew.currentCosm != __instance.currentCosm || !__instance.currentCosm.crew.ContainsKey(crew.id))
						{
							__instance.goalCompleted();
							return false;
						}
						if (__instance.heldSkill == null || !(__instance.heldSkill.GetType() == typeof(HitScanShoot)))
						{
							//__instance.selectGun();
							typeof(Crew).GetMethod("selectGun", flags, null, Type.EmptyTypes, null).Invoke(__instance, null);
							return false;
						}
						___floatRegister += elapsed;
						if (___floatRegister > 0.3f)
						{
							___floatRegister = 0f;
							//___targetLOS = __instance.testLOS(crew.position, __instance.currentCosm);
							var args = new object[] { crew.position, __instance.currentCosm };
							___targetLOS = (bool)typeof(Crew).GetMethod("testLOS", flags, null, new Type[] { typeof(Vector2), typeof(MicroCosm) }, null).Invoke(__instance, args);
						}
						if (___targetLOS)
						{
							__instance.target = crew.position;
							Vector2 vector = __instance.target - __instance.position;
							__instance.rotation = (float)Math.Atan2((double)vector.Y, (double)vector.X) + 1.5707964f;
							__instance.heldSkill.activate(__instance, __instance.currentCosm, __instance.target);
							if (___strafeSpot == Vector2.Zero)
							{
								int num = 0;
								int num2 = __instance.currentCosm.nodeAt(__instance.position);
								if (num2 == -1)
								{
									return false;
								}
								SmartNode smartNode = __instance.currentCosm.smartNodes[num2];
								for (int i = 0; i < 8; i++)
								{
									if (smartNode.neighbors[i] != null && smartNode.neighbors[i].passable)
									{
										num++;
									}
								}
								int num3 = RANDOM.Next(num);
								int num4 = -1;
								int num5 = -1;
								//fixing: a rare crash while crew firing their weapon
								while (num5 < num3 && num4 < 7)
								{
									num4++;
									if (smartNode.neighbors[num4] != null && smartNode.neighbors[num4].passable)
									{
										num5++;
									}
								}
								if (smartNode.neighbors[num4] == null || !smartNode.neighbors[num4].passable)
								{
									return false;
								}
								___strafeSpot = __instance.currentCosm.walkingLocation(smartNode.neighbors[num4].index);
								return false;
							}
							else
							{
								float num6 = __instance.speed / 3f * elapsed;
								if (Vector2.Distance(__instance.position, ___strafeSpot) < num6)
								{
									__instance.position = ___strafeSpot;
									___strafeSpot = Vector2.Zero;
									return false;
								}
								__instance.position += Vector2.Normalize(___strafeSpot - __instance.position) * num6;
								return false;
							}
						}
						else
						{
							___strafeSpot = Vector2.Zero;
							if (__instance.path != null && __instance.path.Count > 1)
							{
								if (___floatRegister == 0f)
								{
									__instance.target = __instance.currentCosm.walkingLocation(__instance.path.First<int>());
									if (Vector2.Distance(__instance.target, crew.position) > 256f)
									{
										int num7 = (int)(crew.position.X / 16f);
										int num8 = (int)(crew.position.Y / 16f);
										int d = num7 + num8 * __instance.currentCosm.width;
										__instance.path = __instance.plotTilePath(d, 1);
										if (__instance.path == null)
										{
											return false;
										}
										__instance.ETA = __instance.path.Count;
									}
								}
								__instance.goTo(1f, elapsed);
								return false;
							}
							if (___floatRegister == 0f)
							{
								int num9 = (int)(crew.position.X / 16f);
								int num10 = (int)(crew.position.Y / 16f);
								int d2 = num9 + num10 * __instance.currentCosm.width;
								try
								{
									__instance.path = __instance.plotTilePath(d2, 1);
								}
								catch
								{
									return false;
								}
								if (__instance.path == null)
								{
									return false;
								}
								__instance.ETA = __instance.path.Count;
								return false;
							}
						}
					}
					else if (__instance.goal.GetType() == typeof(Monster))
					{
						Monster monster = __instance.goal as Monster;
						if (monster.dead)
						{
							__instance.goalCompleted();
							return false;
						}

						if (__instance.heldSkill == null || !(__instance.heldSkill.GetType() == typeof(HitScanShoot)))
						{
							//fixing: followers are not equiping their weapon and attacking a monster if they see one and have some other tool equiped.
							typeof(Crew).GetMethod("selectGun", flags, null, Type.EmptyTypes, null).Invoke(__instance, null);
							return false;
						}

						if (__instance.heldSkill != null && __instance.heldSkill.GetType() == typeof(HitScanShoot))
						{
							
							___floatRegister += elapsed;
							if (___floatRegister > 0.3f)
							{
								___floatRegister = 0f;
								//___targetLOS = __instance.testLOS(monster.position, __instance.currentCosm);
								var args = new object[] { monster.position, __instance.currentCosm };
								___targetLOS = (bool)typeof(Crew).GetMethod("testLOS", flags, null, new Type[] { typeof(Vector2), typeof(MicroCosm) }, null).Invoke(__instance, args);

							}
							if (___targetLOS)
							{
								__instance.target = monster.position;
								Vector2 vector2 = __instance.target - __instance.position;
								__instance.rotation = (float)Math.Atan2((double)vector2.Y, (double)vector2.X) + 1.5707964f;
								__instance.heldSkill.activate(__instance, __instance.currentCosm, __instance.target);
								if (___strafeSpot == Vector2.Zero)
								{
									int num11 = 0;
									SmartNode smartNode2 = __instance.currentCosm.smartNodes[__instance.currentCosm.nodeAt(__instance.position)];
									for (int j = 0; j < 8; j++)
									{
										if (smartNode2.neighbors[j] != null && smartNode2.neighbors[j].passable)
										{
											num11++;
										}
									}
									int num12 = RANDOM.Next(num11);
									int num13 = -1;
									int num14 = -1;
									//fixing: a rare crash while crew firing their weapon
									while (num14 < num12 && num13 < 7)
									{
										num13++;
										if (smartNode2.neighbors[num13] != null && smartNode2.neighbors[num13].passable)
										{
											num14++;
										}
									}
									if (smartNode2.neighbors[num13] == null || !smartNode2.neighbors[num13].passable)
									{
										return false;
									}
									___strafeSpot = __instance.currentCosm.walkingLocation(smartNode2.neighbors[num13].index);
									return false;
								}
								else
								{
									float num15 = __instance.speed / 3f * elapsed;
									if (Vector2.Distance(__instance.position, ___strafeSpot) < num15)
									{
										__instance.position = ___strafeSpot;
										___strafeSpot = Vector2.Zero;
										return false;
									}
									__instance.position += Vector2.Normalize(___strafeSpot - __instance.position) * num15;
									return false;
								}
							}
							else
							{
								___strafeSpot = Vector2.Zero;
								if (__instance.path != null && __instance.path.Count > 1)
								{
									if (___floatRegister == 0f)
									{
										__instance.target = __instance.currentCosm.walkingLocation(__instance.path.First<int>());
										if (Vector2.Distance(__instance.target, monster.position) > 256f)
										{
											int num16 = (int)(monster.position.X / 16f);
											int num17 = (int)(monster.position.Y / 16f);
											int d3 = num16 + num17 * __instance.currentCosm.width;
											__instance.path = __instance.plotTilePath(d3, 1);
											if (__instance.path == null)
											{
												return false;
											}
											__instance.ETA = __instance.path.Count;
										}
									}
									__instance.goTo(1f, elapsed);
									return false;
								}
								if (___floatRegister == 0f)
								{
									int num18 = (int)(monster.position.X / 16f);
									int num19 = (int)(monster.position.Y / 16f);
									int d4 = num18 + num19 * __instance.currentCosm.width;
									try
									{
										__instance.path = __instance.plotTilePath(d4, 1);
									}
									catch
									{
										return false;
									}
									if (__instance.path == null)
									{
										return false;
									}
									__instance.ETA = __instance.path.Count;
									return false;
								}
							}
						}
					}
					else
					{
						__instance.goalFailed();
					}
					return false;
				}
			}
		}

		//fixing: a rare crash on AI trying to use turret metrics even if the ship has no turrets
		//(the code in "if (this.avgBulletSpeed > 0f)" block needs a null check for shipConsoleMetric.turrets in vanilla)
		[HarmonyPatch(typeof(ConsoleThought), "FiringActions")]
		public class ConsoleThought_FiringActions
		{

			[HarmonyPrefix]
			private static void Prefix(Ship ship, Console console, ref float ___avgBulletSpeed, ref float __state)
			{

				__state = ___avgBulletSpeed;
				if (ship.shipMetric.ConsoleMetrics[console]?.turrets == null)
				{
					___avgBulletSpeed = 0f;
				}
			}

			[HarmonyPostfix]
			private static void Postfix(ref float ___avgBulletSpeed, float __state)
			{
				___avgBulletSpeed = __state;
			}
		}

		//fixing: gives player instructions how to properly use repair station.
		[HarmonyPatch(typeof(CoOpSpRpG.Console), "givePlayerControl")] 
		public class Console_givePlayerControl
		{

			[HarmonyPrefix]
			private static void Prefix(CoOpSpRpG.Console __instance)
			{
				if (__instance.ship.GetType() == typeof(Station) && PLAYER.currentGame != null && __instance.ship.id != PLAYER.currentGame.homeBaseId)
				{
					if (PLAYER.currentSession.GetType() == typeof(BattleSessionSP))
					{
						Color[] botD = __instance.ship.botD;
						for (int i = 0; i < botD.Length; i++)
						{
							Module module = TILEBAG.getModule(botD[i]);
							if (module != null && module.type == ModuleType.drone_launcher && (module as DroneLauncher).spellID == 32U)
							{
								SCREEN_MANAGER.widgetChat.AddMessage("This station offers repair services, please hail from your ship to request repairs.", MessageTarget.Ship);
								break;
							}
						}
					}
				}
			}
		}


		//fixing: crash after loading savegame in a gauntlet boss battle and saving again after killing the boss, caused by icons not loading properly
		[HarmonyPatch(typeof(BattleSessionG))]
		[HarmonyPatch(MethodType.Constructor)]
		[HarmonyPatch(new Type[] { typeof(PlanetGauntlet), typeof(BinaryReader), typeof(Ship) })]
		public class BattleSessionG_BattleSessionG
		{

			[HarmonyPostfix]
			private static void Postfix(BattleSessionG __instance)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

				for (int i = 0; i < __instance.challenges.Count; i++)
				{
					__instance.challenges[i].icon = __instance.icons.Find((icon) => icon.position == (Vector2)typeof(GauntletChallengeRev2).GetField("position", flags).GetValue(__instance.challenges[i]));
				}
			}
		}

		//fixing crash on saving and loading gauntlet in a boss battle where only 2 enemies are left
		[HarmonyPatch(typeof(GauntletChallengeRev2), "update")]
		public class GauntletChallengeRev2_update
		{
			[HarmonyPrefix]
			private static void Prefix(GauntletChallengeRev2 __instance, ref float[] ___timers)
			{
				if (__instance.currentStage == GauntletChallengeStage.boss_battle && ___timers == null)
				{
					___timers = new float[1];
				}
			}
		}

		//fixing: game crash in gauntlet challange on saving a tuning kit reward
		[HarmonyPatch(typeof(GauntletTuningKit))]
		[HarmonyPatch(MethodType.Constructor)]
		[HarmonyPatch(new Type[] { typeof(int) })]
		public class GauntletTuningKit_GauntletTuningKit
		{

			[HarmonyPostfix]
			private static void Postfix(GauntletTuningKit __instance) 
			{
				__instance.type = InventoryItemType.gauntlet_tuning_kit;
			}
		}

		//crew on NPC ships will now reload missile factories with grey goo if they have any in their inventory
		[HarmonyPatch(typeof(CrewManager), "checkConsoles")]
		public class CrewManager_checkConsoles
		{
			[HarmonyPostfix]
			private static void Postfix(CrewManager __instance)
			{
				bool flag = true;
				if (__instance.currentCosm.ship != null && __instance.currentCosm.ship.id == PLAYER.currentTeam.ownedShip)
				{
					flag = false;
				}
				else
				{
					using (IEnumerator<Crew> enumerator = __instance.currentCosm.crew.Values.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current.isPlayer)
							{
								flag = false;
							}
						}
					}
				}
				if (flag && __instance.currentCosm.cargoBays != null && __instance.currentCosm.cargoBays.Count > 0 && __instance.currentCosm.modules.Exists((item) => item.GetType() == typeof(MissileFactory) && (item as MissileFactory).goo < (double)(item as MissileFactory).gooUse))
				{
					foreach (var cargobay in __instance.currentCosm.cargoBays)
					{
						if (cargobay.storage != null && cargobay.storage.countItemByType(InventoryItemType.grey_goo) == 0)
						{
							bool flag2 = false;
							foreach (byte key in __instance.currentCosm.crew.Keys)
							{
								if (flag2)
								{
									break;
								}
								if (__instance.currentCosm.crew[key].state == CrewState.idle && !__instance.currentCosm.crew[key].isPlayer && __instance.currentCosm.crew[key].countItemOfType(InventoryItemType.grey_goo) >= 6 )
								{
									ModTile[] tiles = cargobay.tiles;
									for (int i = 0; i < tiles.Length; i++)
									{
										if (!tiles[i].blocking)
										{
											__instance.currentCosm.crew[key].setGoal(cargobay.tiles[0]);
											flag2 = true;
											break;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		//crew on NPC ships will now reload missile factories with grey goo if they have any in their inventory
		[HarmonyPatch(typeof(Crew), "setGoal")]
		public class Crew_setGoal
		{
			[HarmonyPrefix]
			private static bool Prefix(Crew __instance, ModTile doIt)
			{
				if (__instance.isPlayer || (__instance.currentCosm != null && __instance.currentCosm.isStation))
				{
					return true;
				}
				__instance.goalFailed();
				if (__instance.state != CrewState.dead)
				{
					__instance.state = CrewState.idle;
					if (doIt.owner != null && doIt.owner.GetType() == typeof(CargoBay) && doIt.owner.functioning && !(doIt.repairable && (doIt.A < 255 || (doIt.owner != null && doIt.owner.hitpoints < doIt.owner.hitpointsMax))))
					{
						Module owner = doIt.owner;
						if (__instance.checkRange(doIt))
						{
							CargoBay cargobay = owner as CargoBay;
							if (cargobay.storage != null)
							{
								int num = 0;
								num += __instance.countItemOfType(InventoryItemType.grey_goo) / 2;
								__instance.deleteByType(InventoryItemType.grey_goo, num);
								for (int i = 0; i < num; i++)
								{
									cargobay.storage.placeInFirstSlot(new InventoryItem(InventoryItemType.grey_goo));
								}
								return false;
							}
							__instance.goalFailed();
							return false;
						}
						__instance.goal = doIt.X;
						__instance.path = __instance.plotTilePath(doIt.X, 1);
						if (__instance.path != null)
						{
							__instance.ETA = __instance.path.Count;
							__instance.state = CrewState.moving;
						}
					}
				}
				return true;
			}
		}

		//fixing: placing some grey goo for any spawning ships which have missile factories and not enough crew to reload them
		[HarmonyPatch(typeof(WorldActor), "getShip", new Type[] { typeof(byte) })]
		public class WorldActor_getShip
		{
			[HarmonyPostfix]
			private static void Postfix(ref Ship __result)
			{
				if (__result != null && __result.faction != 2UL && __result.cosm?.crew != null && __result.cosm.crew.Count > 0)
				{
					if (__result.cosm.modules != null && __result.cosm.modules.Exists((item) => item.GetType() == typeof(MissileFactory)) && __result.cosm.crew.Count <= __result.cosm.consoles.Count)
					{
						if (__result.cosm.cargoBays != null && __result.cosm.cargoBays.Count > 0 && __result.cosm.cargoBays.TrueForAll((bay) => bay.storage != null && bay.storage.countItemByType(InventoryItemType.grey_goo) == 0))
						{
							foreach (var cargobay in __result.cosm.cargoBays)
							{
								if (cargobay.storage != null && cargobay.storage.countItemByType(InventoryItemType.grey_goo) == 0)
								{
									int amount = __result.cosm.modules.FindAll((item) => item.GetType() == typeof(MissileMagazine)).Count * RANDOM.Next(1, 3);
									for (int i = 0; i < amount; i++)
									{
										cargobay.storage.placeInFirstSlot(new InventoryItem(InventoryItemType.grey_goo));
									}
									break;
								}
							}
							int crewtoadd = __result.cosm.consoles.Count - __result.cosm.crew.Count;
							for (int i = 0; i < crewtoadd; i++)
							{
								__result.cosm.addOneCrew();
							}
						}
					}		
				}
			}
		}


		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(SSCMegaFortQuest), "spawnBoss")]
		public class SSCMegaFortQuest_spawnBoss
		{
			[HarmonyPrefix]
			private static bool Prefix(SSCMegaFortQuest __instance, ref bool __result, CallFunctionStage stage, Point ___grid, Vector2 ___position, ref ulong ___targetID, ref bool ___bossKilled)
			{
				WorldActor worldActor = new WorldActor(111, 3UL, ___position, 1f);
				worldActor.grid = ___grid;
				ulong uid = PLAYER.currentWorld.getUID();
				worldActor.id = uid;
				worldActor.data = new CosmMetaData();
				worldActor.data.crew = new Crew[0];
				worldActor.dominantTeam = new CrewTeam();
				worldActor.dominantTeam.ownedShip = uid;
				worldActor.dominantTeam.threats.Add(CONFIG.playerFaction);
				worldActor.dominantTeam.goalType = ConsoleGoalType.kill_enemies;
				worldActor.dominantTeam.aggroRadius = 8000f;
				worldActor.crewOutfitQuality = 30f;
				for (int j = 0; j < 12; j++)
				{
					Crew crew3 = new Crew();
					crew3.id = (byte)(j);
					crew3.outfit(18f, 15f);
					crew3.faction = 3UL;
					crew3.factionless = false;
					crew3.team = worldActor.dominantTeam;
					worldActor.data.addCrew(crew3);
				}
				worldActor.data.buildStorage(worldActor);
				if (worldActor.data.storage != null)
				{
					for (int j = 0; j < 800; j++)
					{
						worldActor.data.addItem(new InventoryItem(InventoryItemType.grey_goo));
					}
				}
				___targetID = worldActor.id;
				if (__instance.targets == null)
				{
					__instance.targets = new List<ulong>();
				}
				__instance.targets.Add(worldActor.id);
				___bossKilled = false;
				PLAYER.currentWorld.spawnActor(worldActor);
				stage.nextStage = null;
				__result = false;
				return false; //instruction for harmony to supress executing the original method
			}
		}

		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(SSCMegaFortQuest), "test")]
		public class SSCMegaFortQuest_test
		{
			[HarmonyPostfix]
			private static void Postfix(SSCMegaFortQuest __instance, Point ___grid, Vector2 ___position, ulong ___targetID,ref bool ___bossKilled)
			{
				if (__instance.stage == 0)
				{
					__instance.currentStage = __instance.allStages[1];
				}				
				if (__instance.stage == 2 && PLAYER.currentSession.grid == ___grid && ___targetID != 0UL && (!PLAYER.currentSession.allShips.ContainsKey(___targetID) || (PLAYER.currentSession.allShips.ContainsKey(___targetID) && (PLAYER.currentSession.allShips[___targetID].cosm?.crew == null || (PLAYER.currentSession.allShips[___targetID].cosm?.crew != null && PLAYER.currentSession.allShips[___targetID].cosm.crew.IsEmpty)))))
				{
					___bossKilled = true;
				}

			}
		}

		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(SSCMegaFortQuest), "clearEventSubscriptions")]
		public class SSCMegaFortQuest_clearEventSubscriptions
		{
			[HarmonyPostfix]
			private static void Postfix(SSCMegaFortQuest __instance)
			{
				__instance.targets = null;
			}
		}


		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(MissileBossQuest), "spawnBoss")]
		public class MissileBossQuest_spawnBoss
		{
			[HarmonyPrefix]
			private static bool Prefix(MissileBossQuest __instance, ref bool __result, CallFunctionStage stage, Point ___grid, Vector2 ___position, ref ulong ___targetID, ref bool ___bossKilled)
			{
				WorldActor worldActor = new WorldActor(91, 3UL, ___position, 1f);
				worldActor.grid = ___grid;
				ulong uid = PLAYER.currentWorld.getUID();
				worldActor.id = uid;
				worldActor.data = new CosmMetaData();
				worldActor.data.crew = new Crew[0];
				worldActor.dominantTeam = new CrewTeam();
				worldActor.dominantTeam.ownedShip = uid;
				worldActor.dominantTeam.threats.Add(CONFIG.playerFaction);
				worldActor.dominantTeam.goalType = ConsoleGoalType.kill_enemies;
				worldActor.dominantTeam.aggroRadius = 8000f;
				worldActor.crewOutfitQuality = 26f;
				for (int j = 0; j < 10; j++)
				{
					Crew crew3 = new Crew();
					crew3.id = (byte)(j);
					crew3.outfit(18f, 15f);
					crew3.faction = 3UL;
					crew3.factionless = false;
					crew3.team = worldActor.dominantTeam;
					worldActor.data.addCrew(crew3);
				}
				worldActor.data.buildStorage(worldActor);
				if (worldActor.data.storage != null)
				{
					for (int j = 0; j < 800; j++)
					{
						worldActor.data.addItem(new InventoryItem(InventoryItemType.grey_goo));
					}
				}
				___targetID = worldActor.id;
				if (__instance.targets == null)
				{
					__instance.targets = new List<ulong>();
				}
				__instance.targets.Add(worldActor.id);
				___bossKilled = false;
				PLAYER.currentWorld.spawnActor(worldActor);
				stage.nextStage = null;
				__result = false;
				return false; //instruction for harmony to supress executing the original method
			}
		}

		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(MissileBossQuest), "test")]
		public class MissileBossQuest_test
		{
			[HarmonyPostfix]
			private static void Postfix(MissileBossQuest __instance, Point ___grid, Vector2 ___position, ulong ___targetID,ref bool ___bossKilled)
			{
				if (__instance.stage == 0)
				{
					__instance.currentStage = __instance.allStages[1];
				}
				if (__instance.stage == 2 && PLAYER.currentSession.grid == ___grid && ___targetID != 0UL && (!PLAYER.currentSession.allShips.ContainsKey(___targetID) || (PLAYER.currentSession.allShips.ContainsKey(___targetID) && (PLAYER.currentSession.allShips[___targetID].cosm?.crew == null || (PLAYER.currentSession.allShips[___targetID].cosm?.crew != null && PLAYER.currentSession.allShips[___targetID].cosm.crew.IsEmpty)))))
				{
					___bossKilled = true;
				}
			}
		}

		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(MissileBossQuest), "clearEventSubscriptions")]
		public class MissileBossQuest_clearEventSubscriptions
		{
			[HarmonyPostfix]
			private static void Postfix(MissileBossQuest __instance)
			{
				__instance.targets = null;
			}
		}



		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(DroneBossQuest), "spawnBoss")]
		public class DroneBossQuest_spawnBoss
		{
			[HarmonyPrefix]
			private static bool Prefix(DroneBossQuest __instance, ref bool __result, CallFunctionStage stage, Point ___grid, Vector2 ___position, ref ulong ___targetID, ref bool ___bossKilled)
			{
				WorldActor worldActor = new WorldActor(106, 3UL, ___position, 1f);
				worldActor.grid = ___grid;
				ulong uid = PLAYER.currentWorld.getUID();
				worldActor.id = uid;
				worldActor.data = new CosmMetaData();
				worldActor.data.crew = new Crew[0];
				worldActor.dominantTeam = new CrewTeam();
				worldActor.dominantTeam.ownedShip = uid;
				worldActor.dominantTeam.threats.Add(CONFIG.playerFaction);
				worldActor.dominantTeam.goalType = ConsoleGoalType.kill_enemies;
				worldActor.dominantTeam.aggroRadius = 8000f;
				worldActor.crewOutfitQuality = 28f;
				for (int j = 0; j < 10; j++)
				{
					Crew crew3 = new Crew();
					crew3.id = (byte)(j);
					crew3.outfit(18f, 15f);
					crew3.faction = 3UL;
					crew3.factionless = false;
					crew3.team = worldActor.dominantTeam;
					worldActor.data.addCrew(crew3);
				}
				___targetID = worldActor.id;
				if (__instance.targets == null)
				{
					__instance.targets = new List<ulong>();
				}
				__instance.targets.Add(worldActor.id);
				___bossKilled = false;
				PLAYER.currentWorld.spawnActor(worldActor);
				stage.nextStage = null;
				__result = false;
				return false; //instruction for harmony to supress executing the original method
			}
		}

		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(DroneBossQuest), "test")]
		public class DroneBossQuest_test
		{
			[HarmonyPostfix]
			private static void Postfix(DroneBossQuest __instance, Point ___grid, Vector2 ___position, ulong ___targetID, ref bool ___bossKilled)
			{
				if (__instance.stage == 0)
				{
					__instance.currentStage = __instance.allStages[1];
				}
				if (__instance.stage == 2 && PLAYER.currentSession.grid == ___grid && ___targetID != 0UL && (!PLAYER.currentSession.allShips.ContainsKey(___targetID) || (PLAYER.currentSession.allShips.ContainsKey(___targetID) && (PLAYER.currentSession.allShips[___targetID].cosm?.crew == null || (PLAYER.currentSession.allShips[___targetID].cosm?.crew != null && PLAYER.currentSession.allShips[___targetID].cosm.crew.IsEmpty)))))
				{
					___bossKilled = true;
				}
			}
		}

		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(DroneBossQuest), "clearEventSubscriptions")]
		public class DroneBossQuest_clearEventSubscriptions
		{
			[HarmonyPostfix]
			private static void Postfix(DroneBossQuest __instance)
			{
				__instance.targets = null;
			}
		}

		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(SiegeQuest), "buildStages")]
		public class SiegeQuest_buildStages
		{
			[HarmonyPrefix]
			private static bool Prefix(SiegeQuest __instance, uint stage)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
				__instance.allStages = new QuestStage[9];
				__instance.allStages[0] = new CallFunctionStage(new CallFunctionStage.CallFunctionStageFunction(__instance.spawnSSCFleetStage));
				__instance.allStages[1] = new WaitSecondsStage(30f);
				//__instance.allStages[2] = new DoConvoStage(__instance.helpCallDialogue());
				__instance.allStages[2] = new DoConvoStage(new DoConvoStage.DialogueCreationFunc(() => typeof(SiegeQuest).GetMethod("helpCallDialogue", flags, null, Type.EmptyTypes, null).Invoke(__instance, null) as DialogueSelectRev2));
				__instance.allStages[3] = new WaitSecondsStage(0.5f);
				//__instance.allStages[4] = new DoConvoStage(__instance.oneFollowUp());
				__instance.allStages[4] = new DoConvoStage(new DoConvoStage.DialogueCreationFunc(() => typeof(SiegeQuest).GetMethod("oneFollowUp", flags, null, Type.EmptyTypes, null).Invoke(__instance, null) as DialogueSelectRev2));
				__instance.allStages[5] = new CallFunctionStage(new CallFunctionStage.CallFunctionStageFunction(__instance.addOtherQuestStage));
				__instance.allStages[6] = new CallFunctionStage(new CallFunctionStage.CallFunctionStageFunction(__instance.waitForAllDeadStage));
				__instance.allStages[6].tip = new ToolTip();
				__instance.allStages[6].tip.tip = "End the siege";
				__instance.allStages[6].tip.description = "The SSC is laying siege to pirate's cove with a number of large warships. Well what are you waiting for? Go blow them up!";
				//__instance.tipStat = (__instance.allStages[8].tip.addStat("Ships destroyed", "", false) as TipStatSmall);
				typeof(SiegeQuest).GetField("tipStat", flags).SetValue(__instance, (__instance.allStages[6].tip.addStat("Ships destroyed", "", false) as TipStatSmall));
				//__instance.allStages[7] = new DoConvoStage(__instance.finishDialogue());
				__instance.allStages[7] = new DoConvoStage(new DoConvoStage.DialogueCreationFunc(() => typeof(SiegeQuest).GetMethod("finishDialogue", flags, null, Type.EmptyTypes, null).Invoke(__instance, null) as DialogueSelectRev2));
				__instance.allStages[8] = new CallFunctionStage(new CallFunctionStage.CallFunctionStageFunction(__instance.giveRewardStage));
				uint num = 0U;
				while ((ulong)num < (ulong)((long)__instance.allStages.Length))
				{
					__instance.allStages[(int)num].stage = num;
					num += 1U;
				}
				__instance.currentStage = __instance.allStages[(int)stage];
				return false; //instruction for harmony to supress executing the original method
			}
		}


		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(SiegeQuest), "spawnSSCFleetStage")]
		public class SiegeQuest_spawnSSCFleetStage
		{
			[HarmonyPrefix]
			private static bool Prefix(ref bool __result, CallFunctionStage stage, ref Point ___grid, List<ulong> ___enemies)
			{
				FriendlyPirateFaction friendlyPirateFaction = null;
				foreach (FactionControllerRev2 factionControllerRev in PLAYER.currentWorld.factions)
				{
					if (factionControllerRev.GetType() == typeof(FriendlyPirateFaction))
					{
						friendlyPirateFaction = (factionControllerRev as FriendlyPirateFaction);
					}
				}
				___grid = friendlyPirateFaction.homeGrid;
				foreach (Vector2 vector in friendlyPirateFaction.siegePoints)
				{
					WorldActor worldActor = new WorldActor(68, 3UL, vector, 1f);
					worldActor.grid = friendlyPirateFaction.homeGrid;
					ulong uid = PLAYER.currentWorld.getUID();
					worldActor.data = new CosmMetaData();
					worldActor.data.crew = new Crew[0];
					worldActor.id = uid;
					worldActor.dominantTeam = new CrewTeam();
					worldActor.dominantTeam.ownedShip = uid;
					worldActor.dominantTeam.goalType = ConsoleGoalType.patrol;
					worldActor.dominantTeam.destinationGrid = friendlyPirateFaction.homeGrid;
					worldActor.dominantTeam.destination = vector;
					for (int j = 0; j < 4; j++)
					{
						Crew crew3 = new Crew();
						crew3.id = (byte)(j);
						crew3.outfit(18f, 15f);
						crew3.faction = 3UL;
						crew3.factionless = false;
						crew3.team = worldActor.dominantTeam;
						worldActor.data.addCrew(crew3);
					}
					___enemies.Add(worldActor.id);
					PLAYER.currentWorld.spawnActor(worldActor);
				}
				stage.nextStage = null;
				__result = false;
				return false; //instruction for harmony to supress executing the original method
			}
		}

		//fixing: quest logic for all of the main questline after killing Budd
		[HarmonyPatch(typeof(CHARACTER_DATA), "addLog")]
		public class CHARACTER_DATA_addLog
		{
			[HarmonyPrefix]
			private static bool Prefix(LogEntryRev2 log)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
				SQLiteCommand sqliteCommand = new SQLiteCommand("insert or replace into logs (name, path, title, description) values ('" + CHARACTER_DATA.selected + "', @path, @title, @description)", typeof(CHARACTER_DATA).GetField("dBCon", flags).GetValue(null) as SQLiteConnection);
				sqliteCommand.Parameters.Add("@path", DbType.String, 32).Value = log.file;
				sqliteCommand.Parameters.Add("@title", DbType.String, 32).Value = log.name;
				sqliteCommand.Parameters.Add("@description", DbType.String, 32).Value = log.description;
				sqliteCommand.ExecuteNonQuery();
				return false; //instruction for harmony to supress executing the original method
			}
		}


		//fixing: bug that prevented quest completion dialogue after killing Greg
		[HarmonyPatch]
		public class GaryVsGregRev2_test
		{
			static MethodBase TargetMethod()
			{
				var type = AccessTools.TypeByName("CoOpSpRpG.GaryVsGregRev2");
				return AccessTools.Method(type, "test");
			}
			[HarmonyPostfix]
			private static void GaryVsGregRev2_test_Postfix(TriggerEvent __instance, ref DialogueSelectRev2 ___dialogue, ref uint ___stage)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

				if (___dialogue != null && ___dialogue.removeMe)
				{
					if (___stage == 4U && SCREEN_MANAGER.dialogue == null)
					{
						___stage = 5U;
						typeof(TriggerEvent).Assembly.GetType("CoOpSpRpG.GaryVsGregRev2").GetMethod("doFinishConvo", flags, null, Type.EmptyTypes, null).Invoke(__instance, null);
					}
				}
				return;
			}
		}


		//fixing bug that prevented spawning Budd and Greg, preventing their quest from completion.

		[HarmonyPatch(typeof(KillStationPirates), "spawnBudd")]
		public class KillStationPirates_spawnBudd
		{
			[HarmonyPrefix]
			private static bool Prefix()
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

				if (PLAYER.currentSession.valuesOfInterest != null)
				{
					for (int i = 0; i < PLAYER.currentSession.valuesOfInterest.Length; i++)
					{
						if (PLAYER.currentSession.valuesOfInterest[i] == "ctp_big_station")
						{
							Vector2 position = PLAYER.currentSession.pointsOfInterest[i];
							WorldActor worldActor = SHIPBAG.makeTemplate(95);
							worldActor.id = PLAYER.currentWorld.getUID();
							ulong id = worldActor.id;
							worldActor.faction = 4UL;
							worldActor.position = position;
							worldActor.rotation = RANDOM.randomRotation();
							worldActor.hackingAvailable = 0f;
							worldActor.data = new CosmMetaData();
							worldActor.data.crew = new Crew[0];
							worldActor.dominantTeam = new CrewTeam();
							worldActor.dominantTeam.aggroRadius = 6000f;
							worldActor.dominantTeam.threats.Add(2UL);
							worldActor.dominantTeam.threats.Add(3UL);
							worldActor.dominantTeam.threats.Add(5UL);
							Crew crew = new Crew();
							crew.id = 0;
							crew.name = "Budd";
							crew.questTag = "kill_budd";
							crew.heldItem = new Gun(19f, GunSpawnFlags.force_special);
							crew.heldArmor = new CrewArmor(17f, ArmorSpawnFlags.no_oxygen | ArmorSpawnFlags.force_heavy);
							crew.faction = 4UL;
							crew.factionless = false;
							crew.team = worldActor.dominantTeam;
							worldActor.data.addCrew(crew);
							Crew crew2 = new Crew();
							crew2.name = "Greg";
							crew2.id = 1;
							crew2.questTag = "gary_v_greg";
							crew2.heldItem = new Gun(19f, GunSpawnFlags.force_shotgun);
							crew2.heldArmor = new CrewArmor(17f, ArmorSpawnFlags.no_oxygen | ArmorSpawnFlags.force_heavy);
							crew2.faction = 4UL;
							crew2.factionless = false;
							crew2.team = worldActor.dominantTeam;
							worldActor.data.addCrew(crew2);
							for (int j = 0; j < 4; j++)
							{
								Crew crew3 = new Crew();
								crew3.id = (byte)(j + 2);
								crew3.outfit(18f, 15f);
								crew3.faction = 4UL;
								crew3.factionless = false;
								crew3.team = worldActor.dominantTeam;
								worldActor.data.addCrew(crew3);
							}
							worldActor.data.buildStorage(worldActor);
							if (worldActor.data.storage != null)
							{
								for (int j = 0; j < 500; j++)
								{
									worldActor.data.addItem(new InventoryItem(InventoryItemType.grey_goo));
								}
							}
							Ship ship = worldActor.getShip(0);
							if (PLAYER.currentShip != null)
							{
								worldActor.dominantTeam.focus = PLAYER.currentShip.id;
								worldActor.dominantTeam.goalType = ConsoleGoalType.kill_target;
							}
							SCREEN_MANAGER.widgetChat.AddMessage("Budd ship has arrived!", MessageTarget.Command);
							PLAYER.currentSession.addLocalShip(ship, SessionEntry.preexisting);
							ship.cosm.init();
							ship.cosm.rearm = true;
							foreach (TriggerEvent triggerEvent in PLAYER.currentGame.activeQuests)
							{
								if (triggerEvent.GetType() == typeof(KillBuddQuestRev2))
								{
									(triggerEvent as KillBuddQuestRev2).buddID = id;
								}
								if (triggerEvent.GetType() == typeof(TriggerEvent).Assembly.GetType("CoOpSpRpG.GaryVsGregRev2"))
								{
									//(triggerEvent as GaryVsGregRev2).buddID = id;
									typeof(TriggerEvent).Assembly.GetType("CoOpSpRpG.GaryVsGregRev2").GetField("buddID", flags).SetValue(triggerEvent, id);
								}
							}
							return false;
						}
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
			private static bool Prefix(Crew ___representative, List<ResponseImmediateAction> ___results, List<StationServices> ___stationServices)
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

				if (___stationServices != null && ___representative.faction == 2UL)         //fixing: after using a console on a repair station it changes faction and no longer offers any repair services if you hail it.
				{
					___representative.faction = 5UL;
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
			private static void Postfix(KeyboardState __state)
			{
				KeyboardState newstate = Keyboard.GetState();
				if(PLAYER.avatar != null &&
				__state.IsKeyDown(CONFIG.keyBindings[0].bind) && newstate.IsKeyUp(CONFIG.keyBindings[0].bind) 
				|| __state.IsKeyDown(CONFIG.keyBindings[1].bind) && newstate.IsKeyUp(CONFIG.keyBindings[1].bind) 
				|| __state.IsKeyDown(CONFIG.keyBindings[2].bind) && newstate.IsKeyUp(CONFIG.keyBindings[2].bind) 
				|| __state.IsKeyDown(CONFIG.keyBindings[3].bind) && newstate.IsKeyUp(CONFIG.keyBindings[3].bind)
				)
				{

					if (PLAYER.avatar.shallNotPass() && PLAYER.avatar.shuffledBinds())
					{
						var temp = CONFIG.keyBindings[0].bind;
						CONFIG.keyBindings[0].bind = CONFIG.keyBindings[3].bind;
						CONFIG.keyBindings[3].bind = temp;
						temp = CONFIG.keyBindings[1].bind;
						CONFIG.keyBindings[1].bind = CONFIG.keyBindings[2].bind;
						CONFIG.keyBindings[2].bind = temp;
						PLAYER.avatar.shuffledBinds(false);
					}

					if (CONFIG.keyBindings[0].bind == Keys.S && CONFIG.keyBindings[3].bind == Keys.W && CONFIG.keyBindings[1].bind == Keys.D && CONFIG.keyBindings[2].bind == Keys.A) //failsafe to load default keybinds
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
				
					/*
					if (__state.IsKeyDown(Keys.PageDown) && newstate.IsKeyUp(Keys.PageDown))
					{
						var temp = CONFIG.keyBindings[0].bind;
						CONFIG.keyBindings[0].bind = CONFIG.keyBindings[3].bind;
						CONFIG.keyBindings[3].bind = temp;
						temp = CONFIG.keyBindings[1].bind;
						CONFIG.keyBindings[1].bind = CONFIG.keyBindings[2].bind;
						CONFIG.keyBindings[2].bind = temp;
						PLAYER.avatar.shuffledBinds(false);
					}


					if (__state.IsKeyDown(Keys.PageUp) && newstate.IsKeyUp(Keys.PageUp) && CONFIG.keyBindings[0].bind == Keys.S && CONFIG.keyBindings[3].bind == Keys.W && CONFIG.keyBindings[1].bind == Keys.D && CONFIG.keyBindings[2].bind == Keys.A)
					{
						var temp = CONFIG.keyBindings[0].bind;
						CONFIG.keyBindings[0].bind = CONFIG.keyBindings[3].bind;
						CONFIG.keyBindings[3].bind = temp;
						temp = CONFIG.keyBindings[1].bind;
						CONFIG.keyBindings[1].bind = CONFIG.keyBindings[2].bind;
						CONFIG.keyBindings[2].bind = temp;
						PLAYER.avatar.shuffledBinds(false);
					}
					*/
				}
		}

		[HarmonyPatch(typeof(VNavigationRev3), "updateInput")]
		public class VNavigationRev3_updateInput
		{
			[HarmonyPrefix]
			private static void Prefix(ref KeyboardState __state, KeyboardState ___oldState)
			{
				__state = ___oldState;
			}

			[HarmonyPostfix]
			private static void Postfix(KeyboardState __state)
			{
				KeyboardState newstate = Keyboard.GetState();
				if (PLAYER.avatar != null &&
				__state.IsKeyDown(CONFIG.keyBindings[0].bind) && newstate.IsKeyUp(CONFIG.keyBindings[0].bind)
				|| __state.IsKeyDown(CONFIG.keyBindings[1].bind) && newstate.IsKeyUp(CONFIG.keyBindings[1].bind)
				|| __state.IsKeyDown(CONFIG.keyBindings[2].bind) && newstate.IsKeyUp(CONFIG.keyBindings[2].bind)
				|| __state.IsKeyDown(CONFIG.keyBindings[3].bind) && newstate.IsKeyUp(CONFIG.keyBindings[3].bind)
				)
				{

					if (PLAYER.avatar.shallNotPass() && PLAYER.avatar.shuffledBinds())
					{
						var temp = CONFIG.keyBindings[0].bind;
						CONFIG.keyBindings[0].bind = CONFIG.keyBindings[3].bind;
						CONFIG.keyBindings[3].bind = temp;
						temp = CONFIG.keyBindings[1].bind;
						CONFIG.keyBindings[1].bind = CONFIG.keyBindings[2].bind;
						CONFIG.keyBindings[2].bind = temp;
						PLAYER.avatar.shuffledBinds(false);
					}

					if (CONFIG.keyBindings[0].bind == Keys.S && CONFIG.keyBindings[3].bind == Keys.W && CONFIG.keyBindings[1].bind == Keys.D && CONFIG.keyBindings[2].bind == Keys.A) //failsafe to load default keybinds
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

				/*
				if (__state.IsKeyDown(Keys.PageDown) && newstate.IsKeyUp(Keys.PageDown))
				{
					var temp = CONFIG.keyBindings[0].bind;
					CONFIG.keyBindings[0].bind = CONFIG.keyBindings[3].bind;
					CONFIG.keyBindings[3].bind = temp;
					temp = CONFIG.keyBindings[1].bind;
					CONFIG.keyBindings[1].bind = CONFIG.keyBindings[2].bind;
					CONFIG.keyBindings[2].bind = temp;
					PLAYER.avatar.shuffledBinds(false);
				}


				if (__state.IsKeyDown(Keys.PageUp) && newstate.IsKeyUp(Keys.PageUp) && CONFIG.keyBindings[0].bind == Keys.S && CONFIG.keyBindings[3].bind == Keys.W && CONFIG.keyBindings[1].bind == Keys.D && CONFIG.keyBindings[2].bind == Keys.A)
				{
					var temp = CONFIG.keyBindings[0].bind;
					CONFIG.keyBindings[0].bind = CONFIG.keyBindings[3].bind;
					CONFIG.keyBindings[3].bind = temp;
					temp = CONFIG.keyBindings[1].bind;
					CONFIG.keyBindings[1].bind = CONFIG.keyBindings[2].bind;
					CONFIG.keyBindings[2].bind = temp;
					PLAYER.avatar.shuffledBinds(false);
				}
				*/
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
					/*
					if (__instance.docked.airlock.cosm.crew.Values.Contains(PLAYER.avatar))
					{
						Crew crew;
						__instance.docked.airlock.cosm.crew.TryRemove(__instance.docked.airlock.cosm.crew.FirstOrDefault(x => x.Value == PLAYER.avatar).Key, out crew);
					}
					*/
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


		[HarmonyPatch(typeof(ConsoleThought), "navUpdate")] // AI for ally escorting player, fixing AI bug
		public class ConsoleThought_navUpdate
		{
			[HarmonyPrefix]
			private static bool Prefix(ConsoleThought __instance, ConcurrentQueue<CrewInteriorAlert> interiorAlerts, BattleSession session, Ship ship, CoOpSpRpG.Console console, float elapsed, ref float ___assesmentTimer, ref float ___closeAirlockCountdown,
			ref float ___engagementTimer, ref ulong ___lastCheckOnHP, ref float ___alternateActionTimer, ref bool ___dronesOut, ref float ___errorRefresh, ref float ___beamErrorRefresh, ref float ___pathfidingUpdateTimer, ref float ___navigationTimer,
			ref Ship ___aimTarget, float ___navigationUpdatePeriod)
			{
				BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
				if (__instance.state == ConsoleState.running) // bug fixing
				{
					___assesmentTimer += elapsed;
					if (___assesmentTimer > 60f)
					{
						___assesmentTimer = 0f;
						if (console != null)
						{
							console.ammoConservation = true;
						}
						__instance.assessed = false;
						if (__instance.owner.team != null && __instance.owner.team.goalType != __instance.goalType)
						{
							__instance.goalType = __instance.owner.team.goalType;
							__instance.state = ConsoleState.idle;
						}
						if (ship.dockingActive && ___closeAirlockCountdown < -1f)
						{
							___closeAirlockCountdown = 120f;
						}
					}
					___engagementTimer += elapsed;
					if (___engagementTimer > 5f)
					{
						___engagementTimer = 0f;
						ulong num = ship.checkMassFast();
						if (Math.Abs((long)(num - ___lastCheckOnHP)) >= 300L)
						{
							var args = new object[] { ship, console };
							typeof(ConsoleThought).GetMethod("ReassesEngagement", flags, null, new Type[] { typeof(Ship), typeof(CoOpSpRpG.Console) }, null).Invoke(__instance, args);
							//__instance.ReassesEngagement(ship, console);
							___lastCheckOnHP = num;
						}
					}
					if (___closeAirlockCountdown > 0f && __instance.owner.faction != 2UL && session.GetType() != typeof(BattleSessionSC))
					{
						___closeAirlockCountdown -= elapsed;
						if (___closeAirlockCountdown <= 0f)
						{
							___closeAirlockCountdown = -100f;
							ship.toggleDocking(false);
						}
					}
					else if (__instance.owner.faction == 2UL && session.GetType() != typeof(BattleSessionSC))
					{
						ship.toggleDocking(true);
					}
					var arg = new object[] { ship, console, true };
					var dmethod = typeof(ConsoleThought).GetMethod("assesShip", flags, null, new Type[] { typeof(Ship), typeof(CoOpSpRpG.Console), typeof(Boolean) }, null);
					var assessed = (bool)dmethod.Invoke(__instance, arg);
					//if (!__instance.assessed && __instance.assesShip(ship, console, true))
					if (!__instance.assessed && assessed)
					{
						__instance.state = ConsoleState.idle;
					}
					CrewInteriorAlert alert;
					while (interiorAlerts.TryDequeue(out alert))
					{
						//__instance.handleInteriorAlert(alert, ship);
						var args = new object[] { alert, ship };
						typeof(ConsoleThought).GetMethod("handleInteriorAlert", flags, null, new Type[] { typeof(CrewInteriorAlert), typeof(Ship) }, null).Invoke(__instance, args);

					}
					var args2 = new object[] { session, ship, console, elapsed };
					typeof(ConsoleThought).GetMethod("UpdateCombatState", flags, null, new Type[] { typeof(BattleSession), typeof(Ship), typeof(CoOpSpRpG.Console), typeof(float) }, null).Invoke(__instance, args2);
					//__instance.UpdateCombatState(session, ship, console, elapsed);
					if (___alternateActionTimer > 100f)
					{
						___alternateActionTimer = 0f;
						__instance.state = ConsoleState.idle;
						___dronesOut = false;
					}
					ship.throttle = 0.7f;
					___errorRefresh += elapsed;
					if (___errorRefresh >= 0.9f)
					{
						___errorRefresh = (float)(RANDOM.NextDouble() * 0.1);
						__instance.newError = true;
					}
					___beamErrorRefresh += elapsed;
					if (___beamErrorRefresh >= 6f)
					{
						___beamErrorRefresh = (float)(RANDOM.NextDouble() * 1.0);
						__instance.newBeamError = true;
					}
					___alternateActionTimer += elapsed;
					___pathfidingUpdateTimer += elapsed;
					___navigationTimer += elapsed;
					var args3 = new object[] { session, ship, console, elapsed };
					typeof(ConsoleThought).GetMethod("updateTargetsAndAggro", flags, null, new Type[] { typeof(BattleSession), typeof(Ship), typeof(CoOpSpRpG.Console), typeof(float) }, null).Invoke(__instance, args3);
					//__instance.updateTargetsAndAggro(session, ship, console, elapsed);
					if (__instance.target != null)
					{
						if (__instance.target.faction == CONFIG.deadShipFaction)
						{
							__instance.target = null;
							ship.aggroTarget = 0UL;
						}
						else
						{
							ship.aggroTarget = __instance.target.id;
						}
					}
					else
					{
						ship.aggroTarget = 0UL;
					}
					if (___aimTarget != null && ___aimTarget.faction == CONFIG.deadShipFaction)
					{
						___aimTarget = null;
					}
				}
				if (__instance.state == ConsoleState.running) // bug fixing
				{
					//bugged case
					float num3 = Vector2.Distance(ship.position, __instance.desiredDestination);
					var arg1 = new object[] { ship, console };
					var myflag1 = (bool)typeof(ConsoleThought).GetMethod("AreEnemiesInAggroRange", flags, null, new Type[] { typeof(Ship), typeof(CoOpSpRpG.Console) }, null).Invoke(__instance, arg1);
					//if (__instance.AreEnemiesInAggroRange(ship, console) && num3 > ship.navRadius)
					if (myflag1 && num3 > ship.navRadius)
					{
						var arg2 = new object[] { ship };
						float num4 = (float)typeof(ConsoleThought).GetMethod("CalculateCombatDistance", flags, null, new Type[] { typeof(Ship) }, null).Invoke(__instance, arg2);
						//float num4 = __instance.CalculateCombatDistance(ship);
						num3 = float.PositiveInfinity;
						if (__instance.target != null)
						{
							num3 = (__instance.target.position - ship.position).Length();
						}

						Type moveUpdateDataType = typeof(ConsoleThought).Assembly.GetType("CoOpSpRpG.MovementUpdateData");
						var moveUpdateData = typeof(ConsoleThought).GetField("moveUpdateData", flags).GetValue(__instance);
						//var arg8 = new object[] {};
						var dmethod2 = moveUpdateDataType.GetMethod("Init", flags, null, Type.EmptyTypes, null);
						dmethod2.Invoke(moveUpdateData, null);
						//__instance.moveUpdateData.Init();

						bool bInDesiredRange = (num3 <= num4);
						moveUpdateDataType.GetField("bInDesiredRange", flags).SetValue(moveUpdateData, bInDesiredRange);
						//__instance.moveUpdateData.bInDesiredRange = (num3 <= num4);

						moveUpdateDataType.GetField("PathEnd", flags).SetValue(moveUpdateData, __instance.desiredDestination);
						//__instance.moveUpdateData.PathEnd = __instance.desiredDestination;

						arg2 = new object[] { session, ship, num3 };
						num3 = (float)typeof(ConsoleThought).GetMethod("UpdateActualDestination", flags, null, new Type[] { typeof(BattleSession), typeof(Ship), typeof(float) }, null).Invoke(__instance, arg2);
						//						num3 = __instance.UpdateActualDestination(session, ship, num3);

						if (___pathfidingUpdateTimer > ___navigationUpdatePeriod)
						{
							___pathfidingUpdateTimer = 0f;
							___navigationTimer = 0f;
							var args7 = new object[] { session, ship, console, elapsed };
							typeof(ConsoleThought).GetMethod("UpdatePathfinding", flags, null, new Type[] { typeof(BattleSession), typeof(Ship), typeof(CoOpSpRpG.Console), typeof(float) }, null).Invoke(__instance, args7);
							//__instance.UpdatePathfinding(session, ship, console, elapsed);
						}
						if (___navigationTimer > ___navigationUpdatePeriod / 3f)
						{
							___navigationTimer = 0f;
							var args8 = new object[] { session, ship, console, false };
							typeof(ConsoleThought).GetMethod("updateNavigationPath", flags, null, new Type[] { typeof(BattleSession), typeof(Ship), typeof(CoOpSpRpG.Console), typeof(bool) }, null).Invoke(__instance, args8);

							//__instance.updateNavigationPath(session, ship, console, false);
						}
						ship.throttle = 0.99f;
						bool flag2 = false;
						if (__instance.target != null)
						{
							flag2 = !session.LineTrace(ship.position, __instance.target.position); //<--------------------------------BUG------------------------------------------------this.target needs null check in vanilla code!

						}
						/*
						else
						{
							SCREEN_MANAGER.widgetChat.AddMessage("Debug Message: Community Bug fixes mod prevented a rare AI bug! Please report this message to the mod author on discord.", MessageTarget.Ship);
						}
						*/
						ship.velocity.Length();
						Vector2 vector3 = __instance.actualDestination - ship.position;
						vector3.Normalize();
						var arg4 = new object[] { ship };
						Vector2 vector4 = (Vector2)typeof(ConsoleThought).GetMethod("calculateAvoidanceVector", flags, null, new Type[] { typeof(Ship) }, null).Invoke(__instance, arg4);
						//Vector2 vector4 = ConsoleThought.calculateAvoidanceVector(ship);
						float scaleFactor3 = Math.Min(vector3.Length(), 50f);
						Vector2 worldDir2 = Vector2.Normalize(vector3) * scaleFactor3;
						float num5 = Vector2.Dot(Vector2.Normalize(ship.velocity), vector3);
						float scaleFactor4 = 1f - Math.Max(0.3f, num5);

						if (bInDesiredRange && flag2)
						{
							ship.throttle = 0.99f;
							ship.strafeMove(Vector2.Zero);
							ship.aimMove(__instance.actualDestination);
						}
						else
						{
							ship.throttle = Math.Max(0.5f, (1f - vector4.Length()) * ship.throttle);
							ship.aimMove(__instance.actualDestination);
							if (num5 < 0.8f)
							{
								worldDir2 = 0.5f * -Vector2.Normalize(ship.velocity) * scaleFactor4;
							}
							else
							{
								worldDir2 = Vector2.Zero;
							}
							ship.strafeMove(worldDir2);
						}
						var firstflag = (bool)moveUpdateDataType.GetField("bIsOnPath", flags).GetValue(moveUpdateData);
						var secondflag = (bool)moveUpdateDataType.GetField("bCanSeeDestination", flags).GetValue(moveUpdateData);
						if (vector4 != Vector2.Zero)
						{
							ship.throttle = 0.5f;
							if (flag2)
							{
								ship.throttle = 0.8f;
							}
							//else if (__instance.moveUpdateData.bIsOnPath || __instance.moveUpdateData.bCanSeeDestination)
							else if (firstflag || secondflag)
							{
								ship.throttle = 0.99f;
								vector4 *= 0.3f;
							}
							if (num5 < 0.8f && (!bInDesiredRange || !flag2))
							{
								ship.strafeMove(-(Vector2.Normalize(ship.velocity) * scaleFactor4) + vector4);
							}
							else
							{
								ship.strafeMove(vector4);
							}
						}
						var args5 = new object[] { session, ship, console, elapsed, true, true, true };
						typeof(ConsoleThought).GetMethod("FiringActions", flags, null, new Type[] { typeof(BattleSession), typeof(Ship), typeof(CoOpSpRpG.Console), typeof(float), typeof(bool), typeof(bool), typeof(bool) }, null).Invoke(__instance, args5);
						//__instance.FiringActions(session, ship, console, elapsed, true, true, true);
						return false;
					}
					var arg3 = new object[] { ship, console };
					var myflag3 = (bool)typeof(ConsoleThought).GetMethod("AreEnemiesInAggroRange", flags, null, new Type[] { typeof(Ship), typeof(CoOpSpRpG.Console) }, null).Invoke(__instance, arg3);
					//if (__instance.AreEnemiesInAggroRange(ship, console))
					if (myflag3)
					{
						var arg6 = new object[] { session, ship, console };
						__instance.desiredDestination = (Vector2)typeof(ConsoleThought).GetMethod("GetRunAwayLoc", flags, null, new Type[] { typeof(BattleSession), typeof(Ship), typeof(CoOpSpRpG.Console) }, null).Invoke(__instance, arg6);
						//__instance.desiredDestination = __instance.GetRunAwayLoc(session, ship, console);
						return false;
					}
					console.crew.goalCompleted();
					console.crew = null;
					__instance.state = ConsoleState.idle;
					if (__instance.isInCombat && __instance.combatDuration > 10f)
					{
						__instance.isInCombat = false;
						__instance.isRunningAway = false;
					}
					//__instance.LeaveCombat();
					typeof(ConsoleThought).GetMethod("LeaveCombat", flags, null, Type.EmptyTypes, null).Invoke(__instance, null);
					return false;
				}
				return true;
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
