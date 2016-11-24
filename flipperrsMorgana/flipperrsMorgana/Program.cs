using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using UnsignedEvade; // Credit to Chaos for logic if about to be hit and his spelldatabase





namespace flipperrsMorgana
{
    class Program
    {
        private static Spell.Skillshot Q;
        private static Spell.Skillshot W;
        private static Spell.Targeted E;
        private static Spell.Active R;
        private static AIHeroClient User = Player.Instance;
        private static Menu MorganaMenu, ComboMenu, DrawingsMenu, SpellsToShield, BlackShieldAllies, AutoMenu, HarassMenu, QConfig, LaneClearMenu;
        private static List<Spell.SpellBase> SpellList = new List<Spell.SpellBase>();
        public static List<MissileClient> ProjectileList = new List<MissileClient>();
        public static List<SpellInfo> EnemyProjectileInformation = new List<SpellInfo>();
        public static readonly Random Random = new Random(DateTime.Now.Millisecond);


        static void Main(string[] args)
        {

            Loading.OnLoadingComplete += Loading_OnLoadingComplete;



        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            Chat.Print("<font color='#07667F'>Flipper's Morgana Loaded Successfully</font> ");
            Chat.Print("<font color='#E11610'>Please Report Any Issues in the Thread.</font> ");


            if (User.ChampionName != "Morgana")
            {
                return;
            }
            Q = new Spell.Skillshot(spellSlot: SpellSlot.Q, spellRange: 1150, skillShotType: SkillShotType.Linear,
                    castDelay: 250, spellSpeed: 1200, spellWidth: 70)
            { AllowedCollisionCount = 0 };

            W = new Spell.Skillshot(spellSlot: SpellSlot.W, spellRange: 1000, skillShotType: SkillShotType.Circular,
                    castDelay: 250, spellSpeed: 2200, spellWidth: 200)
            { AllowedCollisionCount = -1 };

            E = new Spell.Targeted(SpellSlot.E, 800);

            R = new Spell.Active(SpellSlot.R, 600);
            R.DamageType = DamageType.Magical;

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);


            MorganaMenu = MainMenu.AddMenu("Morgana", "Morgana");
            QConfig = MorganaMenu.AddSubMenu("QConfig");
            ComboMenu = MorganaMenu.AddSubMenu("ComboMenu");
            HarassMenu = MorganaMenu.AddSubMenu("HarassMenu");
            LaneClearMenu = MorganaMenu.AddSubMenu("LaneClearMenu");
            SpellsToShield = MorganaMenu.AddSubMenu("SpellsToShield");
            BlackShieldAllies = MorganaMenu.AddSubMenu("BlackShieldAllies");
            DrawingsMenu = MorganaMenu.AddSubMenu("Drawings");
            AutoMenu = MorganaMenu.AddSubMenu("AutoRConfig");




            ComboMenu.Add("Q", new CheckBox("Use Q"));
            ComboMenu.Add("W", new CheckBox("Use W"));
            ComboMenu.Add("E", new CheckBox("Use E"));
            ComboMenu.Add("R", new CheckBox("Use R"));
            HarassMenu.Add("Q", new CheckBox("Use Q"));
            HarassMenu.Add("W", new CheckBox("Use W"));
            LaneClearMenu.Add("W", new CheckBox("Use W on minions"));
            AutoMenu.Add("Rcount", new Slider("Use R If Hit Enemy ", 3, 1, 5));
            LaneClearMenu.Add("minionCount", new Slider("Use W if hit minions ", 3, 1, 5));

            foreach (var Spell in SpellList)
            {
                DrawingsMenu.Add(Spell.Slot.ToString(), new CheckBox("Draw " + Spell.Slot));
            }

            foreach (AIHeroClient client in EntityManager.Heroes.Enemies)
            {
                QConfig.Add(client.ChampionName, new CheckBox("Q Enabled on" + client.ChampionName));
                foreach (SpellInfo info in SpellDatabase.SpellList)
                {
                    if (info.ChampionName == client.ChampionName)
                    {
                        EnemyProjectileInformation.Add(info);

                    }
                }
            }

            foreach (AIHeroClient client in EntityManager.Heroes.Allies)
            {

                BlackShieldAllies.Add(client.ChampionName, new CheckBox("Shield" + client.ChampionName));


            }

            foreach (SpellInfo spell in EnemyProjectileInformation)
            {
                Console.WriteLine(spell.SpellName);
                SpellsToShield.Add(spell.MissileName, new CheckBox("Shield" + spell.MissileName));
            }

            Game.OnTick += Game_OnTick;
            Game.OnUpdate += GameOnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Obj_AI_Base.OnUpdatePosition += OnUpdate;

        }


        public static void OnUpdate(GameObject obj, EventArgs args)
        {
            var missile = obj as MissileClient;
            if (missile != null &&
                missile.SpellCaster != null &&
                missile.SpellCaster.IsEnemy &&
                missile.SpellCaster.Type == GameObjectType.AIHeroClient &&
                ProjectileList.Contains(missile))
            {
                ProjectileList.Remove(missile);
                ProjectileList.Add(missile);
            }
        }

        private static void GameOnUpdate(EventArgs args)
        {
            TryToE();
            //   EAllies();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var Spell in SpellList.Where(spell => DrawingsMenu[spell.Slot.ToString()].Cast<CheckBox>().CurrentValue))
            {

                Circle.Draw(Spell.IsReady() ? Color.Chartreuse : Color.Aquamarine, Spell.Range, User);
            }
        }





        private static void Game_OnTick(EventArgs args)
        {

            TryToE();




            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {

                Combo();

            }

            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }

            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }

        }




        private static void Combo()
        {
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var pred = Q.GetPrediction(t);

            if (t == null)
            {
                return;
            }


            if ((ComboMenu["Q"].Cast<CheckBox>().CurrentValue && t.IsValidTarget(Q.Range) && Q.IsReady() && QConfig[t.ChampionName].Cast<CheckBox>().CurrentValue && pred.HitChance >= HitChance.High))
            {
                Q.Cast(t);
            }

            if ((ComboMenu["W"].Cast<CheckBox>().CurrentValue && t.IsValidTarget(W.Range) && W.IsReady() && t.HasBuffOfType(BuffType.Snare) || t.HasBuffOfType(BuffType.Stun)))
            {
                W.Cast(t);
            }


            if (User.Position.CountEnemiesInRange(R.Range) >= AutoMenu["Rcount"].Cast<Slider>().CurrentValue)
            {
                R.Cast();

            }

        }



        private static void LaneClear()
        {
            var farm = EntityManager.MinionsAndMonsters.EnemyMinions.Where(s => s.IsInRange(W.RangeCheckSource ?? User.Position, W.Range));
            var WBestFarmLoc = W.GetBestCircularCastPosition(farm);
            if (WBestFarmLoc.HitNumber >= AutoMenu["Rcount"].Cast<Slider>().CurrentValue && LaneClearMenu["W"].Cast<CheckBox>().CurrentValue)
            {
                W.Cast(WBestFarmLoc.CastPosition);
            }


        }

        private static void Harass()
        {
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var pred = Q.GetPrediction(t);
            if (t.IsValidTarget(Q.Range) && Q.IsReady() && pred.HitChance >= HitChance.High)
            {
                Q.Cast(t);
            }

            if (t.IsValidTarget(W.Range) && W.IsReady() && t.HasBuffOfType(BuffType.Snare) || t.HasBuffOfType(BuffType.Stun))
            {
                W.Cast(t);
            }
        }


        public static void OnCreate(GameObject obj, EventArgs args)
        {
            var missile = obj as MissileClient;
            if (missile != null &&
                missile.SpellCaster != null &&
                missile.SpellCaster.IsEnemy &&
                missile.SpellCaster.Type == GameObjectType.AIHeroClient)
                ProjectileList.Add(missile);
        }

        public static void OnDelete(GameObject obj, EventArgs args)
        {
            if (obj == null)
                return;

            var missile = obj as MissileClient;
            if (missile != null &&
                missile.SpellCaster != null &&
                missile.SpellCaster.IsEnemy &&
                missile.SpellCaster.Type == GameObjectType.AIHeroClient &&
                ProjectileList.Contains(missile))
            {
                ProjectileList.Remove(missile);
            }
        }

        private static void TryToE()
            //credit to Chaos for this logic if about to be hit!
        {
            if (E.IsReady() && E.IsLearned)
                foreach (MissileClient missile in ProjectileList)
                    foreach (SpellInfo info in EnemyProjectileInformation)
                        foreach (var client in EntityManager.Heroes.Allies)
                        {
                            if (ShouldShield(missile, info, client) && CollisionCheck(missile, info, client))
                            {
                                if (info.ChannelType == SpellDatabase.ChannelType.None && E.IsReady() &&
                                    (SpellsToShield[info.MissileName].Cast<CheckBox>().CurrentValue) &&
                                    missile.IsInRange(Player.Instance, Q.Range) && BlackShieldAllies[client.ChampionName].Cast<CheckBox>().CurrentValue)
                                    E.Cast(client);
                                else if (info.ChannelType != SpellDatabase.ChannelType.None && E.IsReady() &&
                                         (SpellsToShield[info.MissileName].Cast<CheckBox>().CurrentValue) &&
                                         missile.IsInRange(Player.Instance, Q.Range) && BlackShieldAllies[client.ChampionName].Cast<CheckBox>().CurrentValue)
                                    E.Cast(client);
                            }
                        }
        }





        public static bool ShouldShield(MissileClient missile, SpellInfo info, AIHeroClient client)
        {
     

            if (missile.SpellCaster.Name != "Diana")
                if (missile.SData.Name != info.MissileName ||
                    !missile.IsInRange(client, 800))
                    return false;


            if (info.ProjectileType == SpellDatabase.ProjectileType.LockOnProjectile
                && missile.Target != client)
                return false;



            return true;
        }


      
        public static bool CollisionCheck(MissileClient missile, SpellInfo info, AIHeroClient client)
        {
            bool variable = Prediction.Position.Collision.LinearMissileCollision(
                client, missile.StartPosition.To2D(), missile.StartPosition.To2D().Extend(missile.EndPosition, info.Range),
                info.MissileSpeed, info.Width, info.Delay);
            return variable;
        }




    }
}