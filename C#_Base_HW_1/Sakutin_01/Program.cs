namespace Sakutin_1
{    
    public enum Fraction
    {
        Neutral,
        Evil,
        Good
    }

    public class Unit
    {
        public struct Characteristics
        {
            private int _attackPower;
            private int _defence;

            public Characteristics(int damage, int defence)
            {
                if (damage < 0)
                    throw new ArgumentOutOfRangeException($"{nameof(damage)} can't be less than zero");

                if (defence < 0 || defence > 100)
                    throw new ArgumentOutOfRangeException($"{nameof(defence)} should be between 0 and 100 inclusive");

                _attackPower = damage;
                _defence = defence;
            }

            public int AttackPower => _attackPower;
            public int Defence => _defence; 
            public float DefenceProcent => _defence * 0.01f;
        }

        private Fraction _fraction;
        private Characteristics _baseCharacteristics;

        private List<IUnitEffect> _currentEffects;

        public Fraction Fraction => _fraction;
        public Characteristics BaseCharacteristics => _baseCharacteristics;

        public Unit(Characteristics characteristics, Fraction fraction, List<IUnitEffect> startEffects)
        {
            _baseCharacteristics = characteristics; 
            _fraction = fraction;

            if (startEffects != null)
                _currentEffects = startEffects;
            else
                _currentEffects = new List<IUnitEffect>();
        }

        public float Attack(Unit target)
        {
            var attackEffects = GetEffects<IAttackEffect>();
            attackEffects.Sort(new UnitEffectsComparer());
            float resultDamage = BaseCharacteristics.AttackPower;

            foreach (var effect in attackEffects)
            {
                resultDamage = effect.ProcessDamageForAttack(this, target, resultDamage);
            }

            return target.TakeDamage(this, resultDamage);
        }

        public float TakeDamage(Unit from, float damage)
        {
            var defenceEffects = GetEffects<IDefenceEffect>();
            defenceEffects.Sort(new UnitEffectsComparer());
            var resultDefence = BaseCharacteristics.Defence;

            foreach (var effect in defenceEffects)
            {
                resultDefence = effect.ProcessDefence(this, from, resultDefence);
            }

            return damage * (1f - resultDefence / 100f);
        }

        public void AddCharacteristicsChagner(IUnitEffect effect)
        {
            _currentEffects.Add(effect);
        }

        public bool RemoveCharacteristicsChanger(IUnitEffect changer)
        {
            return _currentEffects.Remove(changer);
        }

        private List<T> GetEffects<T>() where T : IUnitEffect
        {
            var targetEffects = new List<T>();

            foreach (var effect in _currentEffects)
            {
                if (effect is T targetEffect)
                    targetEffects.Add(targetEffect);
            }

            return targetEffects;
        }
    }

    public interface IUnitEffect
    {
        int Priority { get; }
    }

    public interface IAttackEffect : IUnitEffect 
    {
        float ProcessDamageForAttack(Unit attacker, Unit target, float currentDamage);
    }

    public interface IDefenceEffect : IUnitEffect
    {
        int ProcessDefence(Unit defender, Unit attacker, int currentDefence);
    }

    public class UnitEffectsComparer : IComparer<IUnitEffect>
    {
        public int Compare(IUnitEffect? x, IUnitEffect? y)
        {
            if (x == null || y == null) return 0;
            return x.Priority.CompareTo(y.Priority);
        }
    }

    public class FractionRule : IAttackEffect
    {
        public int Priority => int.MinValue;

        public float ProcessDamageForAttack(Unit attacker, Unit target, float currentDamage)
        {
            if (attacker.Fraction == Fraction.Neutral || target.Fraction == Fraction.Neutral)
                return currentDamage;

            if (attacker.Fraction == target.Fraction)
                return currentDamage * 0.5f;
            else
                return currentDamage * 1.5f;
        }
    }

    public class DefenceAndAttackEffect : IDefenceEffect, IAttackEffect
    {
        private int _priority;
        private float _damageChange;
        private float _defenceChagne;

        public DefenceAndAttackEffect(float defenceChange, float damageChange, int priority)
        {
            if (damageChange < 0 || defenceChange < 0)
                throw new ArgumentOutOfRangeException($"{nameof(damageChange)} and {nameof(defenceChange)} can't be less than zero");

            _damageChange = damageChange;
            _defenceChagne = defenceChange;
            _priority = priority;
        }

        public int Priority => _priority;

        public float ProcessDamageForAttack(Unit attacker, Unit target, float currentDamage)
        {
            return currentDamage * _damageChange;
        }

        public int ProcessDefence(Unit defender, Unit attacker, int currentDefence)
        {
            return Math.Clamp((int)(currentDefence * _defenceChagne), 0, 100);
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var berserkEffect = new DefenceAndAttackEffect(0.2f, 2f, 0);
            var fractionRule = new FractionRule();

            var baseChars = new Unit.Characteristics(15, 15);

            var goodUnit = new Unit(baseChars, Fraction.Good, new List<IUnitEffect> { fractionRule});
            var evilUnit = new Unit(baseChars, Fraction.Evil, new List<IUnitEffect> { fractionRule });
            var neutralUnit = new Unit(baseChars, Fraction.Neutral, new List<IUnitEffect> { berserkEffect });

            Console.WriteLine("Good vs Evil");

            var resultDamage = goodUnit.Attack(evilUnit);
            Console.WriteLine($"Good attacks: evil takes {resultDamage}");

            Console.WriteLine("Good vs Good");
            
            resultDamage = goodUnit.Attack(goodUnit);
            Console.WriteLine($"Good attacks: good takes {resultDamage}");

            Console.WriteLine("Evil vs Neutral");

            resultDamage = evilUnit.Attack(neutralUnit);
            Console.WriteLine($"Evil attacks: Neutral takes {resultDamage}");

            resultDamage = neutralUnit.Attack(evilUnit);
            Console.WriteLine($"Neutral attacks: Evil takes {resultDamage}");

            Console.ReadKey();
        }
    }
}