using System;

namespace Distance.Api
{
    internal class Distance
    {
        private const int EATH_RADIUS_KM = 6371;

        private const int EATH_RADIUS_MI = 3956;

        public GeoLocation A { get; }

        public GeoLocation B { get; }

        public Distance(GeoLocation a, GeoLocation b) => (A, B) = (a, b);

        public double InKm => Between * EATH_RADIUS_KM;

        public double InMiles => Between * EATH_RADIUS_MI;

        private double Between 
        {
            get
            {
                var lon1 = ToRadians(A.Lon);
                var lon2 = ToRadians(B.Lon);
                var lat1 = ToRadians(A.Lat);
                var lat2 = ToRadians(B.Lat);

                double dlon = lon2 - lon1;
                double dlat = lat2 - lat1;
                double d = Math.Pow(Math.Sin(dlat / 2), 2) +
                           Math.Cos(lat1) * Math.Cos(lat2) *
                           Math.Pow(Math.Sin(dlon / 2), 2);

                return 2 * Math.Asin(Math.Sqrt(d));
            }            
        }

        // Angle in 10th of a degree
        private static double ToRadians(double angleIn10thofaDegree) => (angleIn10thofaDegree * Math.PI) / 180;
    }
}
