﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Daramee.Blockar
{
	partial class BlockarObject
	{
		enum BSONType
		{
			EndDoc = 0,
			Double = 0x01,
			String = 0x02,
			Document = 0x03,
			Array = 0x04,
			BinaryData = 0x05,
			Boolean = 0x08,
			UTCTime = 0x09,
			Null = 0x0A,
			Regexp = 0x0B,
			JavascriptCode = 0x0D,
			JavascriptCodeWScope = 0x0F,
			Integer = 0x10,
			Integer64 = 0x12,
		}

		BSONType __BsonGetValueType (object obj)
		{
			if (obj == null) return BSONType.Null;

			Type type = obj.GetType ();

			if (type.IsArray) return BSONType.Array;
			else if (typeof (double) == type || typeof (float) == type || typeof (decimal) == type)
				return BSONType.Double;
			else if (typeof (int) == type || typeof (short) == type || typeof (ushort) == type
				|| typeof (byte) == type || typeof (sbyte) == type)
				return BSONType.Integer;
			else if (typeof (long) == type || typeof (ulong) == type || typeof (uint) == type)
				return BSONType.Integer64;
			else if (typeof (string) == type)
				return BSONType.String;
			else if (typeof (DateTime) == type || typeof (TimeSpan) == type)
				return BSONType.UTCTime;
			else if (typeof (bool) == type)
				return BSONType.Boolean;
			else if (typeof (Regex) == type)
				return BSONType.Regexp;
			else if (typeof (byte []) == type)
				return BSONType.BinaryData;
			else
				return BSONType.Document;
		}

		byte [] __BsonGetBinaryKey (object key)
		{
			if (key is int) return new byte [] { (byte) (int) (object) key };
			else if (key is string)
			{
				MemoryStream s = new MemoryStream ();
				BinaryWriter w = new BinaryWriter (s);
				w.Write ((key as string).Length + 1);
				w.Write (Encoding.UTF8.GetBytes (key as string));
				w.Write ((byte) 0);
				byte [] temp = s.ToArray ();
				s.Dispose ();
				return temp;
			}
			else throw new ArgumentException ("key type must be 'int' or 'string'.");
		}

		#region Serialization
		/// <summary>
		/// BSON 포맷으로 직렬화한다.
		/// </summary>
		/// <param name="stream">직렬화한 데이터를 보관할 Stream 객체</param>
		public void SerializeToBson (Stream stream)
		{
#if NET20
			using (BinaryWriter writer = new BinaryWriter (stream, Encoding.UTF8))
#else
			using (BinaryWriter writer = new BinaryWriter (stream, Encoding.UTF8, true))
#endif
			{
				SerializeToBson (writer);
			}
		}

		/// <summary>
		/// BSON 포맷으로 직렬화한 바이트 배열을 가져온다.
		/// </summary>
		/// <returns>JSON으로 직렬화한 바이트 배열</returns>
		public byte [] ToBsonArray ()
		{
			using (Stream stream = new MemoryStream ())
			{
				SerializeToBson (stream);
				return (stream as MemoryStream).ToArray ();
			}
		}

		/// <summary>
		/// JSON 포맷으로 직렬화한다.
		/// </summary>
		/// <param name="writer">직렬화한 데이터를 보관할 TextWriter 객체</param>
		public void SerializeToBson (BinaryWriter writer)
		{
			writer.Write (0);
			foreach (var obj in objs)
			{
				__BsonObjectToWriter (writer, obj.Key, obj.Value);
			}
			writer.Flush ();
		}

		void __BsonObjectToWriter (BinaryWriter writer, object key, object obj)
		{
			BSONType type;
			writer.Write ((byte) (type = __BsonGetValueType (obj)));
			writer.Write (__BsonGetBinaryKey (key));
			switch (type)
			{
				case BSONType.Document:
					writer.Write ((obj as BlockarObject).ToBsonArray ());
					break;
				case BSONType.Array:
					{
						writer.Write (0);
						var arr = obj as IEnumerable;
						int count = 0;
						foreach (object arrObj in arr)
							__BsonObjectToWriter (writer, count++, arrObj);
					}
					break;
				case BSONType.Double:
					writer.Write (Convert.ToDouble (obj));
					break;
				case BSONType.Integer:
					writer.Write (Convert.ToInt32 (obj));
					break;
				case BSONType.Integer64:
					writer.Write (Convert.ToInt64 (obj));
					break;
				case BSONType.String:
				case BSONType.JavascriptCode:
				case BSONType.Regexp:
					{
						byte [] data = Encoding.UTF8.GetBytes (obj.ToString ());
						writer.Write (data.Length + 1);
						writer.Write (data);
						writer.Write ((byte) 0);
					}
					break;
				case BSONType.UTCTime:
					if (obj is DateTime)
						writer.Write (((DateTime) obj).ToFileTimeUtc ());
					else if (obj is TimeSpan)
						writer.Write (new DateTime (((TimeSpan) obj).Ticks).ToFileTimeUtc ());
					break;
				case BSONType.Boolean:
					writer.Write (Convert.ToBoolean (obj));
					break;
				case BSONType.BinaryData:
					{
						byte [] data = obj as byte [];
						writer.Write (data.Length);
						writer.Write (data);
					}
					break;
			}
		}
#endregion

#region Deserialization
		/// <summary>
		/// BSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="stream">BSON 데이터가 보관된 Stream 객체</param>
		public void DeserializeFromBson (Stream stream)
		{
#if NET20
			using (BinaryReader reader = new BinaryReader (stream, Encoding.UTF8))
#else
			using (BinaryReader reader = new BinaryReader (stream, Encoding.UTF8, true))
#endif
				DeserializeFromBson (reader);
		}

		/// <summary>
		/// BSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="json">BSON 바이트 배열</param>
		public void DeserializeFromBson (byte [] bsonArray)
		{
			using (Stream stream = new MemoryStream (bsonArray))
				DeserializeFromJson (stream);
		}

		/// <summary>
		/// BSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="reader">BSON 데이터를 읽어올 수 있는 BinaryReader 객체</param>
		public void DeserializeFromBson (BinaryReader reader)
		{
			Clear ();
			__BsonParseBsonObject (this, reader);
		}

		static void __BsonParseBsonObject (BlockarObject blockarObject, BinaryReader reader)
		{
			try
			{
				Queue<object> tokenStack = new Queue<object> ();
				bool isParsing = true;
				int dataSize = reader.ReadInt32 ();
				int currentPosition = (int) reader.BaseStream.Position;
				while (isParsing && (reader.BaseStream.Position - currentPosition) != dataSize)
				{
					BSONType rb = (BSONType) reader.ReadByte ();
					if (rb == BSONType.EndDoc) break;

					tokenStack.Enqueue (__BsonGetKeyFromBinary (reader));
					switch (rb)
					{
						case BSONType.EndDoc: isParsing = false; break;
						case BSONType.Double: tokenStack.Enqueue (reader.ReadDouble ()); break;
						case BSONType.String: tokenStack.Enqueue (__BsonGetStringFromBinary (reader)); break;
						case BSONType.Document:
							{
								BlockarObject inner = new BlockarObject ();
								__BsonParseBsonObject (inner, reader);
								tokenStack.Enqueue (inner);
							}
							break;
						case BSONType.Array:
							{
								List<object> arr = new List<object> ();
								__BsonParseBsonArray (arr, reader);
								tokenStack.Enqueue (arr.ToArray ());
							}
							break;
						case BSONType.BinaryData: tokenStack.Enqueue (__BsonGetBinaryFromBinary (reader)); break;
						case BSONType.Boolean: tokenStack.Enqueue (reader.ReadByte () == 0 ? false : true); break;
						case BSONType.UTCTime: tokenStack.Enqueue (DateTime.FromFileTimeUtc (reader.ReadInt64 ())); break;
						case BSONType.Null: tokenStack.Enqueue (null); break;
						case BSONType.Regexp: tokenStack.Enqueue (new Regex (__BsonGetStringFromBinary (reader))); break;
						case BSONType.JavascriptCode: tokenStack.Enqueue (__BsonGetStringFromBinary (reader)); break;
						case BSONType.Integer: tokenStack.Enqueue (reader.ReadInt32 ()); break;
						case BSONType.Integer64: tokenStack.Enqueue (reader.ReadInt64 ()); break;

						default: throw new Exception ("There is unsupport Data type.");
					}
				}

				if (tokenStack.Count % 2 != 0)
					throw new ArgumentException ("Invalid JSON document.");

				while (tokenStack.Count != 0)
				{
					string key = tokenStack.Dequeue () as string;
					object value = tokenStack.Dequeue ();
					blockarObject.Set (key, value);
				}
			}
			catch { throw new ArgumentException ("Invalid JSON document."); }
		}

		static void __BsonParseBsonArray (List<object> arr, BinaryReader reader)
		{
			try
			{
				bool isParsing = true;
				int dataSize = reader.ReadInt32 ();
				int currentPosition = (int) reader.BaseStream.Position;
				while (isParsing && (reader.BaseStream.Position - currentPosition) != dataSize)
				{
					BSONType rb = (BSONType) reader.ReadByte ();
					if (rb == BSONType.EndDoc) break;

					reader.ReadByte ();

					switch (rb)
					{
						case BSONType.EndDoc: isParsing = false; break;
						case BSONType.Double: arr.Add (reader.ReadDouble ()); break;
						case BSONType.String: arr.Add (__BsonGetStringFromBinary (reader)); break;
						case BSONType.Document:
							{
								BlockarObject inner = new BlockarObject ();
								__BsonParseBsonObject (inner, reader);
								arr.Add (inner);
							}
							break;
						case BSONType.Array:
							{
								List<object> innerArr = new List<object> ();
								__BsonParseBsonArray (innerArr, reader);
								arr.Add (innerArr.ToArray ());
							}
							break;
						case BSONType.BinaryData: arr.Add (__BsonGetBinaryFromBinary (reader)); break;
						case BSONType.Boolean: arr.Add (reader.ReadByte () == 0 ? false : true); break;
						case BSONType.UTCTime: arr.Add (DateTime.FromFileTimeUtc (reader.ReadInt64 ())); break;
						case BSONType.Null: arr.Add (null); break;
						case BSONType.Regexp: arr.Add (new Regex (__BsonGetStringFromBinary (reader))); break;
						case BSONType.JavascriptCode: arr.Add (__BsonGetStringFromBinary (reader)); break;
						case BSONType.Integer: arr.Add (reader.ReadInt32 ()); break;
						case BSONType.Integer64: arr.Add (reader.ReadInt64 ()); break;

						default: throw new Exception ("There is unsupport Data type.");
					}
				}
			}
			catch { throw new ArgumentException ("Invalid JSON document."); }
		}

		static string __BsonGetKeyFromBinary (BinaryReader jsonBinary)
		{
			StringBuilder sb = new StringBuilder ();
			char ch;
			while ((ch = jsonBinary.ReadChar ()) != '\0')
				sb.Append (ch);
			return sb.ToString ();
		}

		static string __BsonGetStringFromBinary (BinaryReader jsonBinary)
		{
			int length = jsonBinary.ReadInt32 ();
			return Encoding.UTF8.GetString (jsonBinary.ReadBytes (length), 0, length - 1);
		}

		static byte [] __BsonGetBinaryFromBinary (BinaryReader jsonBinary)
		{
			int length = jsonBinary.ReadInt32 ();
			return jsonBinary.ReadBytes (length);
		}
#endregion
	}
}
