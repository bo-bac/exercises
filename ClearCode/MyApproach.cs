public static partial class PrimeGenerator
{
    public class SieveOfEratosthenesAlgorithm
    {
        private const int FIRST_PRIME = 2;

        public int[] GeneratePrimes(int maxNumber)
        {
            if (maxNumber < FIRST_PRIME)
            {
                return new int[0]; 
            }

            // initialize sieve
            int sieveSize = maxNumber + 1;
            bool[] sieve = Enumerable
                .Range(0, maxNumber)
                .Select(i => i >= FIRST_PRIME)
                .ToArray();

            // algorithm
            for (var i = FIRST_PRIME; i < Math.Sqrt(sieveSize) + 1; i++)
            {
                if (sieve[i]) // if i is uncrossed, cross its multiples
                {
                    for (var j = FIRST_PRIME * i; j < sieveSize; j += i)
                    {
                        sieve[j] = false; //multiple is not prime
                    }
                }
            }

            int count = CountPrimes(sieve);
            var primes = new int[count];
            CollectPrimes(sieve, primes);

            return primes;
        }

        private int CountPrimes(bool[] sieve)
        {
            //...code here..
        }

        private void CollectPrimes(bool[] sieve, int[] primes)
        {
            //...code here..
        }
    }
}