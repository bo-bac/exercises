using System.Diagnostics.CodeAnalysis;

namespace Distance.Api
{
    public readonly struct IATA
    {
        private readonly string code = string.Empty;

        public IATA(string code)
        {
            this.code = code.ToUpperInvariant();
        }

        public bool IsValid() => !string.IsNullOrWhiteSpace(code) && code.Length == 3;

        public static implicit operator string(IATA airport) => airport.code;
        public static explicit operator IATA(string code) => new(code);

        public override string ToString() => code;

        public override bool Equals([NotNullWhen(true)] object obj) =>
            obj is IATA airport && this.code == airport.code;

        public override int GetHashCode() => code.GetHashCode();

        public static bool operator ==(IATA left, IATA right) => left.Equals(right);

        public static bool operator !=(IATA left, IATA right) => !(left == right);

        public static string  InvalidMessage(string name) => $"'{name}' should be 3-letter IATA code";
    }
}
