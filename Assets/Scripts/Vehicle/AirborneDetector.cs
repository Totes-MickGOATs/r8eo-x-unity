using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>Tracks consecutive off-ground frames to determine airborne state.</summary>
    public class AirborneDetector
    {
        readonly int _threshold;
        int _frames;

        public AirborneDetector(int threshold = 5) { _threshold = threshold; }

        public bool Update(bool offGround)
        {
            _frames = offGround ? Mathf.Min(_frames + 1, _threshold) : 0;
            return _frames >= _threshold;
        }
    }
}
