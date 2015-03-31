using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;


namespace Karthus
{
    class Program
    {

        public const string HERO_NAME = "Karthus";
        public static Orbwalking.Orbwalker Orbwalker;

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static Menu Config;
        private static Obj_AI_Hero Hero;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Hero = ObjectManager.Player;
            if (Hero.ChampionName != HERO_NAME)
                return;

            Q = new Spell(SpellSlot.Q, 875f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R, 20000f);

            Q.SetSkillshot(1f, 150f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.25f, 0f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Config = new Menu(HERO_NAME, HERO_NAME, true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.SubMenu("Draw").AddItem(new MenuItem("Draw.Q", "Draw Q").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("Draw.W", "Draw W").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("Draw.E", "Draw E").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("Draw.Target", "Draw Target").SetValue(true));

            Config.AddItem(new MenuItem("LasthitQ", "Last hit with Q").SetValue(true));
            Config.AddItem(new MenuItem("HitChanceQ", "Q HitChance").SetValue(new StringList(new[] { "Low", "Medium", "High", "VeryHigh" }, 2)));
            Config.AddToMainMenu();


            MenuItem drawComboDamageMenu = new MenuItem("Draw.Dmg", "Draw Combo Damage", true).SetValue(true);
            MenuItem drawFill = new MenuItem("Draw.Fill", "Draw Combo Damage Fill", true).SetValue(new Circle(true, System.Drawing.Color.FromArgb(90, 255, 169, 4)));
            Config.SubMenu("Drawings").AddItem(drawComboDamageMenu);
            Config.SubMenu("Drawings").AddItem(drawFill);
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

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private static float GetComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0d;

            if (Q.IsReady())
                damage += Hero.GetSpellDamage(hero, SpellSlot.Q) * 3;

            if (E.IsReady())
                damage += Hero.GetSpellDamage(hero, SpellSlot.E);

            if (R.IsReady())
                damage += Hero.GetSpellDamage(hero, SpellSlot.R);

            return (float)damage;
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var sender = gapcloser.Sender;
            if (sender.IsValidTarget(W.Range) &&
                W.IsReady() && HasMana() &&
                !sender.HasBuffOfType(BuffType.Invulnerability))
                W.Cast(sender, true, true);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            bool alfq = Config.Item("Draw.Q").GetValue<bool>();
            bool alfw = Config.Item("Draw.W").GetValue<bool>();
            bool alfe = Config.Item("Draw.E").GetValue<bool>();
            bool alft = Config.Item("Draw.Target").GetValue<bool>();

            var t = Orbwalker.GetTarget();

            if (alfq)
                Render.Circle.DrawCircle(Hero.Position, Q.Range, System.Drawing.Color.White);
            if (alfw)
                Render.Circle.DrawCircle(Hero.Position, W.Range, System.Drawing.Color.White);
            if (alfe)
                Render.Circle.DrawCircle(Hero.Position, E.Range, System.Drawing.Color.White);

            if (alft && t.IsValidTarget())
                Render.Circle.DrawCircle(t.Position, 100, System.Drawing.Color.Cyan);

            // Thank you Sebby/Sebastiank1
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => target.Team != Hero.Team))
            {
                if (Q.GetDamage(target) > target.Health)
                {
                    Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                    Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q kill: " + target.ChampionName + " have: " + target.Health + "hp");
                }

                if (R.GetDamage(target) > target.Health && R.IsReady())
                {
                    Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Cyan);
                    Drawing.DrawText(Drawing.Width * 0.2f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "R kill" + target.ChampionName + " have: " + target.Health + "hp");
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode.ToString() == "Combo")
            {
                Obj_AI_Base t = TargetSelector.GetTarget(875f, TargetSelector.DamageType.Magical, true);

                if (W.IsReady())
                {
                    if (t.IsValidTarget(W.Range) &&
                    !t.HasBuffOfType(BuffType.Invulnerability) &&
                    HasMana())
                        W.Cast(t, true, true);
                }

                if (Q.IsReady())
                {
                    if (t.IsValidTarget(Q.Range) &&
                        !t.HasBuffOfType(BuffType.Invulnerability) && HasMana())
                        Q.CastIfHitchanceEquals(t, QHit(), true);
                }

                if (E.IsReady())
                {
                    if (t.IsValidTarget(E.Range) &&
                    !t.HasBuffOfType(BuffType.Invulnerability) &&
                    HasMana())
                        E.Cast();
                }
            }

            if (Orbwalker.ActiveMode.ToString() == "LastHit")
            {
                if (Config.Item("LasthitQ").GetValue<bool>())
                {
                    var minions = MinionManager.GetMinions(Hero.ServerPosition, 875f, MinionTypes.All, MinionTeam.NotAlly);
                    var minion = minions[0];

                    float predictedHealth = HealthPrediction.GetHealthPrediction(minion, (int)(Q.Delay));
                    float dmg = Q.GetDamage(minion);
                    if (predictedHealth < dmg &&
                        minion.IsValidTarget(Q.Range) &&
                        !Orbwalking.InAutoAttackRange(minion) &&
                        HasMana())
                        Q.CastIfHitchanceEquals(minion, QHit(), true);
                }
            }

            if (Orbwalker.ActiveMode.ToString() == "LaneClear" || Orbwalker.ActiveMode.ToString() == "Mixed")
            {
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>()
                    .Where(target => target.Team != Hero.Team))
                {
                    if (target.IsValidTarget(Q.Range) && !target.HasBuffOfType(BuffType.Invulnerability) && HasMana())
                        Q.CastIfHitchanceEquals(target, QHit(), true);
                }

                var minions = MinionManager.GetMinions(Hero.ServerPosition, 875f, MinionTypes.All, MinionTeam.NotAlly);
                var minion = minions[0];

                if (minion.IsValidTarget(Q.Range) && HasMana())
                    Q.CastIfHitchanceEquals(minion, QHit(), true);
                if (minions.Count > 2 && E.IsReady() &&
                    minion.IsValidTarget(E.Range))
                    E.Cast();
            }

            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().
                Where(target => target.Team != Hero.Team))
            {
                if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                    target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) ||
                    target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Slow) ||
                    target.HasBuff("Recall") && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    if (Q.IsReady() && target.IsValidTarget(Q.Range))
                        Q.CastIfHitchanceEquals(target, QHit(), true);
                    else if (W.IsReady() && target.IsValidTarget(W.Range))
                        W.Cast(target, true, true);
                }
            }
        }

        private static bool HasMana()
        {
            if (Hero.Mana > Hero.MaxMana / 0.1)
                return true;
            else
                return false;
        }

        private static HitChance QHit()
        {
            int index = Config.Item("HitChanceQ").GetValue<StringList>().SelectedIndex;

            if (index == 0)
                return HitChance.Low;
            if (index == 1)
                return HitChance.Medium;
            if (index == 2)
                return HitChance.High;
            if (index == 3)
                return HitChance.VeryHigh;
            else
                return HitChance.High;
        }
    }
}
