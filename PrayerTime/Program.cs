using System;
using GoogleMaps.LocationServices;
using System.Globalization;

namespace PrayerTime
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// Reference: Egyptian General Authority of Survey
			// Fajr twilight: -19.5
			// Isha twilight: -17.5

			string address = args [0];

			// Using GoogleMaps.LocationServices class to get Latitude and Longitude.
			var locationService = new GoogleLocationService ();

			var point = locationService.GetLatLongFromAddress (address);
			var latitude = point.Latitude;
			var longitude = point.Longitude;

			const string dataFmt = "{0,-25}{1}";
			const string timeFmt = "{0,-25}{1:yyyy-MM-dd HH:mm}";

			// Get local time zone and current local time and year.
			TimeZone localZone = TimeZone.CurrentTimeZone;
			DateTime currentDate = DateTime.Now;
			int currentYear = currentDate.Year;
			int currentMonth = currentDate.Month;
			int currentDay = currentDate.Day;

			Console.WriteLine (dataFmt, "Standard time name: ", localZone.StandardName);
			Console.WriteLine (timeFmt, "Current date and time: ", currentDate);

			TimeSpan currentOffset = localZone.GetUtcOffset (currentDate);

			int gmtOffset = currentOffset.Hours;

			Console.WriteLine (dataFmt, "UTC offset: ", currentOffset);

			double fajr, sunRise, zuhr, asr, maghrib, isha;
			fajr = 0; sunRise = 0; zuhr = 0; asr = 0; maghrib = 0; isha = 0;

			// Generate prayer times based on given location and local time.
			CalcPrayerTimes (currentYear, currentMonth, currentDay, longitude, latitude, gmtOffset, -19.5, -17.5,
				ref fajr, ref sunRise, ref zuhr, ref asr, ref maghrib, ref isha);

			int hours, minutes;
			hours = 0; minutes = 0;

			Console.WriteLine (dataFmt, "Latitude:", latitude);
			Console.WriteLine (dataFmt, "Longitude:", longitude);
			Console.WriteLine ();

			DoubleToHrMin (fajr, ref hours, ref minutes);
			Console.WriteLine ("Fajr    - {0}:{1}", hours.ToString("00"), minutes.ToString("00"));

			DoubleToHrMin (sunRise, ref hours, ref minutes);
			Console.WriteLine ("Sunrise - {0}:{1}", hours.ToString("00"), minutes.ToString("00"));

			DoubleToHrMin (zuhr, ref hours, ref minutes);
			Console.WriteLine ("Zuhr    - {0}:{1}", hours.ToString("00"), minutes.ToString("00"));

			DoubleToHrMin (asr, ref hours, ref minutes);
			Console.WriteLine ("Asr     - {0}:{1}", hours.ToString("00"), minutes.ToString("00"));

			DoubleToHrMin (maghrib, ref hours, ref minutes);
			Console.WriteLine ("Maghrib - {0}:{1}", hours.ToString("00"), minutes.ToString("00"));

			DoubleToHrMin (isha, ref hours, ref minutes);
			Console.WriteLine ("Isha    - {0}:{1}", hours.ToString("00"), minutes.ToString("00"));
		}

		static void CalcPrayerTimes(int year, int month, int day,
		                            double longitude, double latitude, int timezone,
		                            double fajrTwilight, double ishaTwilight,
		                            ref double fajrTime, ref double sunRiseTime, ref double zuhrTime,
		                            ref double asrTime, ref double maghribTime, ref double ishaTime) {
			double D = (367 * year) - ((year + (int)((month + 9) / 12)) * 7 / 4) + (((int)(275 * month / 9)) + day - 730531.5);

			double L = 280.461 + 0.9856474 * D;
			L = MoreLess360 (L);

			double M = 357.528 + (0.9856003) * D;
			M = MoreLess360 (M);

			double Lambda = L + 1.915 * Math.Sin (DegToRad (M)) + 0.02 * Math.Sin (DegToRad (2 * M));
			Lambda = MoreLess360 (Lambda);

			double Obliquity = 23.439 - 0.0000004 * D;
			double Alpha = RadToDeg (Math.Atan ((Math.Cos (DegToRad (Obliquity)) * Math.Tan (DegToRad (Lambda)))));
			Alpha = MoreLess360 (Alpha);

			Alpha = Alpha - (360 * (int)(Alpha / 360));
			Alpha = Alpha + 90 * (Math.Floor (Lambda / 90) - Math.Floor (Alpha / 90));

			double ST = 100.46 + 0.985647352 * D;
			double Dec = RadToDeg (Math.Asin(Math.Sin(DegToRad(Obliquity)) * Math.Sin(DegToRad(Lambda))));
			double Durinal_Arc = RadToDeg(Math.Acos((Math.Sin(DegToRad(-0.8333)) - Math.Sin(DegToRad(Dec))*Math.Sin(DegToRad(latitude))) / (Math.Cos(DegToRad(Dec)) * Math.Cos(DegToRad(latitude)))));

			double Noon = Alpha - ST;
			Noon = MoreLess360 (Noon);

			double UT_Noon = Noon - longitude;

			// Calculating Prayer Times Arcs & Times

			// 2) Zuhr Time [Local noon]
			zuhrTime = UT_Noon / 15 + timezone;

			// Asr Shafii
			double Asr_Alt = RadToDeg (Math.Atan (1 + Math.Tan (DegToRad (latitude - Dec))));
			double Asr_Arc = RadToDeg(Math.Acos((Math.Sin(DegToRad(90 - Asr_Alt)) - Math.Sin(DegToRad(Dec)) * Math.Sin(DegToRad(latitude))) / (Math.Cos(DegToRad(Dec)) * Math.Cos(DegToRad(latitude)))));
			Asr_Arc = Asr_Arc / 15;

			// 3) Asr Time
			asrTime = zuhrTime + Asr_Arc;

			// 1) Shorouq Time
			sunRiseTime = zuhrTime - (Durinal_Arc / 15);

			// 4) Maghrib Time
			maghribTime = zuhrTime + (Durinal_Arc / 15);

			double Esha_arc = RadToDeg(Math.Acos((Math.Sin(DegToRad(ishaTwilight)) - Math.Sin(DegToRad(Dec)) * Math.Sin(DegToRad(latitude))) / (Math.Cos(DegToRad(Dec)) * Math.Cos(DegToRad(latitude)))));

			// 5) Isha Time
			ishaTime = zuhrTime + (Esha_arc / 15);

			// 0) Fajr Time
			double Fajr_Arc = RadToDeg(Math.Acos((Math.Sin(DegToRad(fajrTwilight)) - Math.Sin(DegToRad(Dec)) * Math.Sin(DegToRad(latitude))) / (Math.Cos(DegToRad(Dec)) * Math.Cos(DegToRad(latitude)))));
			fajrTime = zuhrTime - (Fajr_Arc / 15);

			return;
		}

		// Convert Degree to Radian
		static double DegToRad(double degree) {
			return ((Math.PI / 180) * degree);
		}

		// Convert Radian to Degree
		static double RadToDeg(double radian) {
			return (radian * (180 / Math.PI));
		}

		// Make sure a value is between 0 and 360
		static double MoreLess360(double value) {
			while (value > 360 || value < 0) {
				if (value > 360)
					value -= 360;
				else if (value < 0)
					value += 360;
			}

			return value;
		}

		// Make sure a value is between 0 and 24
		static double MoreLess24(double value) {
			while (value > 24 || value < 0) {
				if (value > 24)
					value -= 24;
				else if (value < 0)
					value += 24;
			}

			return value;
		}

		// Convert double number to Hours and Minutes
		static void DoubleToHrMin(double number, ref int hours, ref int minutes) {
			hours = (int)Math.Floor (MoreLess24(number));
			minutes = (int)Math.Floor (MoreLess24(number - hours) * 60);
		}
	}
}
