using Microsoft.Win32;

namespace ParsElecom.ConfigManager
{
	public class Registry : Profile
	{
		private RegistryKey m_rootKey = Microsoft.Win32.Registry.CurrentUser;
		public Registry()
		{
		}

		public Registry(RegistryKey rootKey, string subKeyName) :
			base("")
		{
			if (rootKey != null)
				m_rootKey = rootKey;
			if (subKeyName != null)
				Name = subKeyName;
		}

		public Registry(Registry reg) :
			base(reg)
		{
			m_rootKey = reg.m_rootKey;
		}

		public override string DefaultName
		{
			get
			{
				return "Software\\ParsElecom\\ConfigManager";			
			}
		}

		public override object Clone()
		{
			return new Registry(this);
		}

		public RegistryKey RootKey
		{
			get 
			{ 
				return m_rootKey; 
			}
			set 
			{ 
				VerifyNotReadOnly();
				if (m_rootKey == value)
					return;
				
				if (!RaiseChangeEvent(true, ProfileChangeType.Other, null, "RootKey", value))
					return;
				
				m_rootKey = value; 
				RaiseChangeEvent(false, ProfileChangeType.Other, null, "RootKey", value);
			}
		}

		protected RegistryKey GetSubKey(string section, bool create, bool writable)		
		{
			VerifyName();
			
			string keyName = Name + "\\" + section;

			if (create)
				return m_rootKey.CreateSubKey(keyName);
			return m_rootKey.OpenSubKey(keyName, writable);
		}

		public override void SetValue(string section, string entry, object value)
		{
			// If the value is null, remove the entry
			if (value == null)
			{
				RemoveEntry(section, entry);
				return;
			}
			
			VerifyNotReadOnly();
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);
			
			if (!RaiseChangeEvent(true, ProfileChangeType.SetValue, section, entry, value))
				return;
			
			using (RegistryKey subKey = GetSubKey(section, true, true))
				subKey.SetValue(entry, value);
			
			RaiseChangeEvent(false, ProfileChangeType.SetValue, section, entry, value);
		}

		public override object GetValue(string section, string entry)
		{
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);

			using (RegistryKey subKey = GetSubKey(section, false, false))
				return (subKey == null ? null : subKey.GetValue(entry));
		}

		public override void RemoveEntry(string section, string entry)
		{
			VerifyNotReadOnly();
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);
			
			using (RegistryKey subKey = GetSubKey(section, false, true))
			{
				if (subKey != null && subKey.GetValue(entry) != null)
				{
					if (!RaiseChangeEvent(true, ProfileChangeType.RemoveEntry, section, entry, null))
						return;
			
					subKey.DeleteValue(entry, false);
					RaiseChangeEvent(false, ProfileChangeType.RemoveEntry, section, entry, null);
				}
			}	
		}

		public override void RemoveSection(string section)
		{
			VerifyNotReadOnly();
			VerifyName();
			VerifyAndAdjustSection(ref section);
			
			using (RegistryKey key = m_rootKey.OpenSubKey(Name, true))
			{
				if (key != null && HasSection(section))
				{
					if (!RaiseChangeEvent(true, ProfileChangeType.RemoveSection, section, null, null))
						return;
					
					key.DeleteSubKeyTree(section);
					RaiseChangeEvent(false, ProfileChangeType.RemoveSection, section, null, null);
				}
			}	
		}
		
		public override string[] GetEntryNames(string section)
		{
			VerifyAndAdjustSection(ref section);

			using (RegistryKey subKey = GetSubKey(section, false, false))
			{
				if (subKey == null)
					return null;
				
				return subKey.GetValueNames();
			}				
		}		

		public override string[] GetSectionNames()
		{
			VerifyName();
			
			using (RegistryKey key = m_rootKey.OpenSubKey(Name))
			{
				if (key == null)
					return null;				
				return key.GetSubKeyNames();
			}				
		}		
	}
}
