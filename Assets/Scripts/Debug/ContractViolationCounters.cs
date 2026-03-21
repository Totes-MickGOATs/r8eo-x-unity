namespace R8EOX.Debug
{
    /// <summary>
    /// Per-session violation counters for <see cref="ContractDebugger"/>.
    /// Groups the four counter fields and their public read properties.
    /// </summary>
    public struct ContractViolationCounters
    {
        // ---- Fields ----

        private int _input;
        private int _vehicle;
        private int _wheel;
        private int _observable;

        // ---- Public Properties ----

        /// <summary>Total input-contract violations this session.</summary>
        public int Input      => _input;

        /// <summary>Total vehicle-contract violations this session.</summary>
        public int Vehicle    => _vehicle;

        /// <summary>Total wheel-contract violations this session.</summary>
        public int Wheel      => _wheel;

        /// <summary>Total observable-contract violations this session.</summary>
        public int Observable => _observable;

        // ---- Mutation ----

        /// <summary>Adds <paramref name="delta"/> to the input counter.</summary>
        public void AddInput(int delta)      => _input      += delta;

        /// <summary>Adds <paramref name="delta"/> to the vehicle counter.</summary>
        public void AddVehicle(int delta)    => _vehicle    += delta;

        /// <summary>Adds <paramref name="delta"/> to the wheel counter.</summary>
        public void AddWheel(int delta)      => _wheel      += delta;

        /// <summary>Adds <paramref name="delta"/> to the observable counter.</summary>
        public void AddObservable(int delta) => _observable += delta;

        /// <summary>Resets all counters to zero.</summary>
        public void Reset()
        {
            _input      = 0;
            _vehicle    = 0;
            _wheel      = 0;
            _observable = 0;
        }
    }
}
