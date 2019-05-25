
namespace NobleMirrorSample
{
    /// <summary>
    /// ダメージを与えられる、ということを示す。破壊可能な箱や敵はこれを継承する
    /// </summary>
    public interface IDamageable
    {
        void DealDamage(int damage);
    }
}