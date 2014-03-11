﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Ini
{
	public sealed partial class IniSection : IEnumerable<KeyValuePair<string, string>>
	{
		Dictionary<string, string> container = new Dictionary<string, string> ();

		public string Name { get; set; }

		public IniSection () { }
		public IniSection ( string iniText ) : this ( new MemoryStream ( Encoding.UTF8.GetBytes ( iniText ) ) ) { }
		public IniSection ( Stream stream )
		{
			IniSection section = Parse ( stream );
			Name = section.Name;
			container = section.container;
		}

		public void Add ( string key, object value )
		{
			if ( key.Length == 0 ) return;
			container.Add ( key, value is string ? value as string : value.ToString () );
		}

		public void Remove ( string key ) { container.Remove ( key ); }

		public string this [ string key ]
		{
			get { return container [ key ]; }
			set { container [ key ] = value; }
		}

		public bool Contains ( string key ) { return container.Keys.Contains ( key ); }

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendLine ( string.Format ( "[{0}]", Name ) );
			foreach ( KeyValuePair<string, string> record in container )
				sb.AppendLine ( string.Format ( record.Value.Contains ( ";" ) ? "{0}=\"{1}\"" : "{0}={1}", record.Key, record.Value ) );
			return sb.ToString ();
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator ()
		{
			return container.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return container.GetEnumerator ();
		}
	}
}
