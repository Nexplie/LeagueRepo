using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//       - Library -
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace Malzahar
{
    class Program
    {

        public const string heroName = "Malzahar";
        public static Orbwalking.Orbwalker Orbwalker;

        public static Spell Q, W, E, R;

        private static Obj_AI_Hero Hero = ObjectManager.Player;

        public static Menu _Menu;
        public static SpellSlot Ignite;

        public static bool Packets;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Hero.BaseSkinName != heroName)
                return;

            // - Set-up Spells 

            Q = new Spell(SpellSlot.Q, 900f);
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 650f);
            R = new Spell(SpellSlot.R, 700f);

            Q.SetSkillshot(.5f, 30, 1600, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.50f, 50, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Ignite = Hero.GetSpellSlot("SummonerDot");

            // - Menu Constructor
            _Menu = new Menu(heroName, heroName, true);

            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _Menu.AddSubMenu(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _Menu.AddSubMenu(targetSelectorMenu);

            // - SubMenus
            _Menu.AddSubMenu(new Menu("Combo", "Combo"));
            _Menu.AddSubMenu(new Menu("Harass", "Harass"));
            _Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _Menu.AddSubMenu(new Menu("Miscellaneous", "Misc"));
            _Menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            _Menu.AddToMainMenu();

            // - Items
            _Menu.SubMenu("Combo").AddItem(new MenuItem("Malzahar.Q.Combo", "Cast Q").SetValue(true));
            _Menu.SubMenu("Combo").AddItem(new MenuItem("Malzahar.W.Combo", "Cast W").SetValue(true));
            _Menu.SubMenu("Combo").AddItem(new MenuItem("Malzahar.E.Combo", "Cast E").SetValue(true));
            _Menu.SubMenu("Combo").AddItem(new MenuItem("Malzahar.R.Combo", "Cast R").SetValue(true));
            _Menu.SubMenu("Combo").AddItem(new MenuItem("Malzahar.Ignite", "Cast Ignite").SetValue(true));
            _Menu.SubMenu("Combo").AddItem(new MenuItem("SEP01", "--------------------"));
            _Menu.SubMenu("Combo").AddItem(new MenuItem("Ignite.Only.Killable", "Ignite Only if Killable").SetValue(true));
            _Menu.SubMenu("Combo").AddItem(new MenuItem("R.Only.Killable", "R Only if Killable").SetValue(true));

            _Menu.SubMenu("Harass").AddItem(new MenuItem("Malzahar.Q.Harass", "Cast Q").SetValue(true));
            _Menu.SubMenu("Harass").AddItem(new MenuItem("Malzahar.E.Harass", "Cast E").SetValue(true));

            _Menu.SubMenu("LaneClear").AddItem(new MenuItem("Malzahar.Q.LaneClear", "Cast Q").SetValue(true));
            _Menu.SubMenu("LaneClear").AddItem(new MenuItem("Malzahar.W.LaneClear", "Cast W").SetValue(true));
            _Menu.SubMenu("LaneClear").AddItem(new MenuItem("Malzahar.E.LaneClear", "Cast E").SetValue(true));

            _Menu.SubMenu("Drawings").AddItem(new MenuItem("Malzahar.Q", "Draw Q Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            _Menu.SubMenu("Drawings").AddItem(new MenuItem("Malzahar.W", "Draw W Range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            _Menu.SubMenu("Drawings").AddItem(new MenuItem("Malzahar.E", "Draw E Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            _Menu.SubMenu("Drawings").AddItem(new MenuItem("Malzahar.R", "Draw R Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));

            _Menu.SubMenu("Misc").AddItem(new MenuItem("Malzahar.QR.Interrupt", "Use Q || R Interrupt").SetValue(true));
            _Menu.SubMenu("Misc").AddItem(new MenuItem("Malzahar.R.GapCloser", "Use R on GapClosers").SetValue(false));
            _Menu.SubMenu("Misc").AddItem(new MenuItem("Malzahar.KS", "Use KS System").SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloserOnOnEnemyGapcloser;


            MenuItem drawComboDamageMenu = new MenuItem("Malzahar.DrawDmg", "Draw Combo Damage", true).SetValue(true);
            MenuItem drawFill = new MenuItem("Malzahar.DrawFill", "Draw Combo Damage Fill", true).SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));
            _Menu.SubMenu("Drawings").AddItem(drawComboDamageMenu);
            _Menu.SubMenu("Drawings").AddItem(drawFill);
            DamageIndicator.DamageToUnit = GetComboDamage;
            DamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
            DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
            DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;
            drawComboDamageMenu.ValueChanged +=
                delegate (object sender, OnValueChangeEventArgs eventArgs)
                {
                    DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };
            drawFill.ValueChanged +=
                delegate (object sender, OnValueChangeEventArgs eventArgs)
                {
                    DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                    DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
                };

            // - Notification
            Notifications.AddNotification("Nexplie Malzahar!", 2000);
        }

        private static void AntiGapcloserOnOnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            Obj_AI_Hero sender = gapcloser.Sender;
            // bool             

            if (R.IsReady())
                if (sender.IsValidTarget(R.Range) &&
                    _Menu.Item("Malzahar.R.GapCloser").GetValue<bool>()) {
                    R.CastOnUnit(sender, false);
                }
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {           
            if (Q.IsReady())
                if (sender.IsValidTarget(Q.Range) &&
                    _Menu.Item("Malzahar.QR.Interrupt").GetValue<bool>()) {
                    Q.CastIfHitchanceEquals(sender, HitChance.Medium, false);
                }
            if (!Q.IsReady() &&
                R.IsReady())
                    if (sender.IsValidTarget(R.Range) &&
                    _Menu.Item("Malzahar.QR.Interrupt").GetValue<bool>()) {
                    R.CastOnUnit(sender, false);
                }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_Menu.Item("Malzahar.Q").GetValue<Circle>().Active) {
                Render.Circle.DrawCircle(Hero.Position, Q.Range, _Menu.Item("Malzahar.Q").GetValue<Circle>().Color);
            }
            if (_Menu.Item("Malzahar.W").GetValue<Circle>().Active) {
                Render.Circle.DrawCircle(Hero.Position, W.Range, _Menu.Item("Malzahar.W").GetValue<Circle>().Color);
            }
            if (_Menu.Item("Malzahar.E").GetValue<Circle>().Active) {
                Render.Circle.DrawCircle(Hero.Position, E.Range, _Menu.Item("Malzahar.E").GetValue<Circle>().Color);
            }
            if (_Menu.Item("Malzahar.R").GetValue<Circle>().Active) {
                Render.Circle.DrawCircle(Hero.Position, R.Range, _Menu.Item("Malzahar.R").GetValue<Circle>().Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            Obj_AI_Hero t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            double qDamage = Hero.GetSpellDamage(t, SpellSlot.Q);
            double wDamage = Hero.GetSpellDamage(t, SpellSlot.W) * 3;
            double eDamage = Hero.GetSpellDamage(t, SpellSlot.E);
            double rDamage = Hero.GetSpellDamage(t, SpellSlot.R);         

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {

                if (Q.IsReady())
                    if (t.IsValidTarget(Q.Range) &&
                        _Menu.Item("Malzahar.Q.Combo").GetValue<bool>()) {
                        Q.CastIfHitchanceEquals(t, HitChance.Medium, false);
                    }
                if (E.IsReady())
                    if (t.IsValidTarget(E.Range) &&
                        _Menu.Item("Malzahar.E.Combo").GetValue<bool>()) { 
                        E.CastOnUnit(t, false);
                    }
                if (W.IsReady())
                    if (t.IsValidTarget(W.Range) &&
                        _Menu.Item("Malzahar.W.Combo").GetValue<bool>()) {
                        W.Cast(t.Position, false);
                    }
                if (R.IsReady())
                    if (t.IsValidTarget(R.Range) &&
                        _Menu.Item("R.Only.Killable").GetValue<bool>() &&
                        _Menu.Item("Malzahar.R.Combo").GetValue<bool>() &&
                        t.Health + 10 < rDamage) {
                        R.CastOnUnit(t, false);
                    }
                else if (R.IsReady())
                        if (t.IsValidTarget(R.Range) &&
                            !_Menu.Item("R.Only.Killable").GetValue<bool>() &&
                            _Menu.Item("Malzahar.R.Combo").GetValue<bool>()) {
                            R.CastOnUnit(t, false);
                        }
                if (Ignite != SpellSlot.Unknown)
                    if (Hero.Spellbook.CanUseSpell(Ignite) == SpellState.Ready &&
                        _Menu.Item("Ignite.Only.Killable").GetValue<bool>() &&
                        _Menu.Item("Malzahar.Ignite").GetValue<bool>()) { 
                        Hero.Spellbook.CastSpell(Ignite, t);
                }
                else if (Ignite != SpellSlot.Unknown)
                        if (Hero.Spellbook.CanUseSpell(Ignite) == SpellState.Ready &&
                            !_Menu.Item("Ignite.Only.Killable").GetValue<bool>() &&
                            _Menu.Item("Malzahar.Ignite").GetValue<bool>()) {
                            Hero.Spellbook.CastSpell(Ignite, t);
                        }                
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (Q.IsReady())
                    if (t.IsValidTarget(Q.Range) &&
                        _Menu.Item("Malzahar.Q.Harass").GetValue<bool>()) {
                        Q.CastIfHitchanceEquals(t, HitChance.Medium, false);
                    }
                if (E.IsReady())
                    if (t.IsValidTarget(E.Range) &&
                        _Menu.Item("Malzahar.E.Harass").GetValue<bool>()) {
                        E.CastOnUnit(t, false);
                    }               
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                foreach (var minion in ObjectManager.Get<Obj_AI_Minion>()
                    .Where(m => m.IsValidTarget()))
                {
                    if (Q.IsReady())
                        if (_Menu.Item("Malzahar.Q.LaneClear").GetValue<bool>() &&
                            minion.IsValidTarget(Q.Range)) {
                            Q.CastIfHitchanceEquals(minion, HitChance.Medium, false);
                        }
                    if (W.IsReady())
                        if (_Menu.Item("Malzahar.W.LaneClear").GetValue<bool>() &&
                            minion.IsValidTarget(W.Range)) {
                            W.Cast(minion.Position, false);
                        }
                    if (E.IsReady())
                        if (_Menu.Item("Malzahar.E.LaneClear").GetValue<bool>() &&
                            minion.IsValidTarget(E.Range)) {
                            E.CastOnUnit(minion, false);
                        }
                }
            }

            if (_Menu.Item("Malzahar.KS").GetValue<bool>())
            {
                foreach (var champ in ObjectManager.Get<Obj_AI_Hero>()
                    .Where(h => h.Team != Hero.Team))
                {
                    if (Q.IsReady())
                        if (champ.IsValidTarget(Q.Range) &&
                            champ.Health + 10 < qDamage) {
                            Q.CastIfHitchanceEquals(champ, HitChance.Medium, false);
                        }
                    if (E.IsReady())
                        if (champ.IsValidTarget(E.Range) &&
                            champ.Health + 10 < eDamage) {
                            E.CastOnUnit(champ, false);
                        } 
                }
            }

            foreach (var ch in ObjectManager.Get<Obj_AI_Hero>()
                .Where(y => y.HasBuffOfType(BuffType.Stun) ||
                y.HasBuffOfType(BuffType.Taunt) ||
                y.HasBuffOfType(BuffType.Suppression)))
            {
                W.Cast(ch.Position, false);
            }
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            double damage = 0d;

            if (Q.IsReady())
                damage += Hero.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += Hero.GetSpellDamage(enemy, SpellSlot.W) * 3;

            if (E.IsReady())
                damage += Hero.GetSpellDamage(enemy, SpellSlot.E);

            if (R.IsReady())
                damage += Hero.GetSpellDamage(enemy, SpellSlot.R);

            return (float)damage;
        }
    }
}
