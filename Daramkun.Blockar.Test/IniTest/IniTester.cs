﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daramkun.Blockar.Ini;

namespace Daramkun.Blockar.Test.IniTest
{
	public static class IniTester
	{
		public static void Run ()
		{
			IniSection section = new IniSection ();
			section.Name = "Test";
			section.Add ( "Key", "Value" );
			section.Add ( "Number", 1234 );
			section.Add ( "FloatingPoint", 1234.5678 );
			section.Add ( "CutTheComment", "asdfasdfsa; asdf" );
			section.Add ( "", "Any value" );
			section.Add ( "ip", "123.123.123.123" );
			section.Add ( "port", 12345 );

			Console.WriteLine ( "=========== Original INI ===========" );
			Console.WriteLine ( section );

			Console.WriteLine ( "=========== Parsed INI ===========" );
			foreach ( IniSection ini in IniParser.Parse ( section.ToString () ) )
				Console.WriteLine ( ini );

			Console.WriteLine ( "=========== Benchmark ===========" );
			int loopCount = 100000;
			string iniString = section.ToString ();
			
			int start = Environment.TickCount;
			for ( int i = 0; i < loopCount; i++ )
			{
				IniParser.Parse ( section.ToString () );
			}

			int end = Environment.TickCount;
			Console.WriteLine ( String.Format ( "위 INI 데이터 {0:0,0}번 파싱하는데 걸린 시간 : {1:0.000}sec", loopCount, ( end - start ) / 1000.0f ) );
		}
	}
}
