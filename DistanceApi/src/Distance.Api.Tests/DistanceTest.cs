using FluentAssertions;
using Xunit;

namespace Distance.Api.Tests
{
    public class DistanceTest
    {
        [Fact]
        public void distance_between_ams_and_vlc_should_be_921_mi()
        {
            //Arrange      
            var ams = new GeoLocation { Lat = 52.309069, Lon = 4.763385 };
            var vlc = new GeoLocation { Lat = 39.491792, Lon = -0.473475 };

            //Act
            var expected = new Distance(ams, vlc).InMiles;

            //Assert
            expected.Should().BeApproximately(921, 2);
        }

        [Fact]
        public void distance_between_ams_and_vlc_should_be_1482_km()
        {
            //Arrange      
            var ams = new GeoLocation { Lat = 52.309069, Lon = 4.763385 };
            var vlc = new GeoLocation { Lat = 39.491792, Lon = -0.473475 };

            //Act
            var expected = new Distance(ams, vlc).InKm;

            //Assert
            expected.Should().BeApproximately(1482, 2);
        }

    }
}
