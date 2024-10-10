using System;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices; 
using System.Reflection;
using System.IO;

namespace ParsElecom.ConfigManager
{
	public class Ini : Profile
	{
		public Ini()
		{
		}

		public Ini(string fileName) :
			base(fileName)
		{
		}

		public Ini(Ini ini) :
			base(ini)
		{
		}

		public override string DefaultName
		{
			get
			{
				return DefaultNameWithoutExtension + ".ini";
			}
		}

		public override object Clone()
		{
			return new Ini(this);
		}

		[DllImport("kernel32", SetLastError=true)]
        static extern int WritePrivateProfileString(string section, string key, string value, string fileName);
        [DllImport("kernel32", SetLastError=true)]
		static extern int WritePrivateProfileString(string section, string key, int value, string fileName);
        [DllImport("kernel32", SetLastError=true)]
        static extern int WritePrivateProfileString(string section, int key, string value, string fileName);
        [DllImport("kernel32")]
        static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder result, int size, string fileName);
        [DllImport("kernel32")]
        static extern int GetPrivateProfileString(string section, int key, string defaultValue, [MarshalAs(UnmanagedType.LPArray)] byte[] result, int size, string fileName);
        [DllImport("kernel32")]
        static extern int GetPrivateProfileString(int section, string key, string defaultValue, [MarshalAs(UnmanagedType.LPArray)] byte[] result, int size, string fileName);

		public override void SetValue(string section, string entry, object value)
		{
			if (value == null)
			{
				RemoveEntry(section, entry);
				return;
			}
			
			VerifyNotReadOnly();
			VerifyName();
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);
			
			if (!RaiseChangeEvent(true, ProfileChangeType.SetValue, section, entry, value))
				return;
							
			if (WritePrivateProfileString(section, entry, value.ToString(), Name) == 0)
				throw new Win32Exception();

			RaiseChangeEvent(false, ProfileChangeType.SetValue, section, entry, value);
		}

		public override object GetValue(string section, string entry)
		{
			VerifyName();
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);

			for (int maxSize = 250; true; maxSize *= 2)
			{
				StringBuilder result = new StringBuilder(maxSize);
            	int size = GetPrivateProfileString(section, entry, "", result, maxSize, Name);
				
				if (size < maxSize - 1)
				{					
					if (size == 0 && !HasEntry(section, entry))
						return null;
					return result.ToString();
				}
			}
		}

		public override void RemoveEntry(string section, string entry)
		{
			if (!HasEntry(section, entry))
				return;
				
			VerifyNotReadOnly();
			VerifyName();
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);
			
			if (!RaiseChangeEvent(true, ProfileChangeType.RemoveEntry, section, entry, null))
				return;
			
			if (WritePrivateProfileString(section, entry, 0, Name) == 0)
				throw new Win32Exception();

			RaiseChangeEvent(false, ProfileChangeType.RemoveEntry, section, entry, null);
		}

		public override void RemoveSection(string section)
		{
			if (!HasSection(section))
				return;
			
			VerifyNotReadOnly();
			VerifyName();
			VerifyAndAdjustSection(ref section);
			
			if (!RaiseChangeEvent(true, ProfileChangeType.RemoveSection, section, null, null))
				return;
			
			if (WritePrivateProfileString(section, 0, "", Name) == 0)
				throw new Win32Exception();

			RaiseChangeEvent(false, ProfileChangeType.RemoveSection, section, null, null);
		}

		public override string[] GetEntryNames(string section)
		{
			if (!HasSection(section))
				return null;

			VerifyAndAdjustSection(ref section);
			    
			for (int maxSize = 500; true; maxSize *= 2)
			{
				byte[] bytes = new byte[maxSize];				
            	int size = GetPrivateProfileString(section, 0, "", bytes, maxSize, Name);
				
				if (size < maxSize - 2)
				{
					string entries = Encoding.ASCII.GetString(bytes, 0, size - (size > 0 ? 1 : 0));			
					if (entries == "")
						return new string[0];
		            return entries.Split(new char[] {'\0'});			
				}
			}
		}

		public override string[] GetSectionNames()
		{
			if (!File.Exists(Name))
				return null;
			
			for (int maxSize = 500; true; maxSize *= 2)
			{
				byte[] bytes = new byte[maxSize];				
            	int size = GetPrivateProfileString(0, "", "", bytes, maxSize, Name);
				
				if (size < maxSize - 2)
				{
					string sections = Encoding.ASCII.GetString(bytes, 0, size - (size > 0 ? 1 : 0));			
					if (sections == "")
						return new string[0];
		            return sections.Split(new char[] {'\0'});			
				}
			}
		}
	}
}
