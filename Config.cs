using System.IO;
using System.Xml;

namespace ParsElecom.ConfigManager
{
	public class Config : XmlBased
	{
		private string m_groupName = "profile";
		private const string SECTION_TYPE = "System.Configuration.NameValueSectionHandler, System, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, Custom=null";

		public Config()
		{
		}

		public Config(string fileName) :
			base(fileName)
		{
		}

		public Config(Config config) :
			base(config)
		{
			m_groupName = config.m_groupName;
		}

		public override string DefaultName
		{
			get
			{
				return DefaultNameWithoutExtension + ".config";
			}
		}

		public override object Clone()
		{
			return new Config(this);
		}

		public string GroupName
		{
			get 
			{ 
				return m_groupName; 
			}
			set 
			{ 
				VerifyNotReadOnly();
				if (m_groupName == value)
					return;

				if (!RaiseChangeEvent(true, ProfileChangeType.Other, null, "GroupName", value))
					return;

				m_groupName = value; 
				if (m_groupName != null)
				{
					m_groupName = m_groupName.Replace(' ', '_');

					if (m_groupName.IndexOf(':') >= 0)
						throw new XmlException("نام گروه قابل قبول نیست.");
				}

				RaiseChangeEvent(false, ProfileChangeType.Other, null, "GroupName", value);				
			}
		}

		private bool HasGroupName
		{
			get
			{
				return m_groupName != null && m_groupName != "";
			}
		}
		
		private string GroupNameSlash
		{
			get 
			{ 
				return (HasGroupName ? (m_groupName + "/") : "");
			}
		}

		private bool IsAppSettings(string section)
		{
			return !HasGroupName && section != null && section == "appSettings";
		}

		protected override void VerifyAndAdjustSection(ref string section)
		{
			base.VerifyAndAdjustSection(ref section);
			if (section.IndexOf(' ') >= 0)
				section = section.Replace(' ', '_');
		}

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
			
			bool hasGroupName = HasGroupName;
			bool isAppSettings = IsAppSettings(section);
			
			if ((m_buffer == null || m_buffer.IsEmpty) && !File.Exists(Name))
			{				
				XmlTextWriter writer = null;
				
				if (m_buffer == null)
					writer = new XmlTextWriter(Name, Encoding);			
				else
					writer = new XmlTextWriter(new MemoryStream(), Encoding);			

				writer.Formatting = Formatting.Indented;
	            
	            writer.WriteStartDocument();
				
	            writer.WriteStartElement("configuration");			
				if (!isAppSettings)
				{
					writer.WriteStartElement("configSections");
					if (hasGroupName)
					{
						writer.WriteStartElement("sectionGroup");
						writer.WriteAttributeString("name", null, m_groupName);				
					}
					writer.WriteStartElement("section");
					writer.WriteAttributeString("name", null, section);				
					writer.WriteAttributeString("type", null, SECTION_TYPE);
        			writer.WriteEndElement();

					if (hasGroupName)
            			writer.WriteEndElement();
           			writer.WriteEndElement();
				}
				if (hasGroupName)
					writer.WriteStartElement(m_groupName);
				writer.WriteStartElement(section);
				writer.WriteStartElement("add");
				writer.WriteAttributeString("key", null, entry);				
				writer.WriteAttributeString("value", null, value.ToString());
    			writer.WriteEndElement();
    			writer.WriteEndElement();
				if (hasGroupName)
           			writer.WriteEndElement();
       			writer.WriteEndElement();
			
				if (m_buffer != null)
					m_buffer.Load(writer);
				writer.Close();   				

				RaiseChangeEvent(false, ProfileChangeType.SetValue, section, entry, value);
				return;
			}
			
			
			XmlDocument doc = GetXmlDocument();
			XmlElement root = doc.DocumentElement;
			
			XmlAttribute attribute = null;
			XmlNode sectionNode = null;
			
			if (!isAppSettings)
			{
				XmlNode sectionsNode = root.SelectSingleNode("configSections");
				if (sectionsNode == null)
					sectionsNode = root.AppendChild(doc.CreateElement("configSections"));			
	
				XmlNode sectionGroupNode = sectionsNode;
				if (hasGroupName)
				{
					sectionGroupNode = sectionsNode.SelectSingleNode("sectionGroup[@name=\"" + m_groupName + "\"]");
					if (sectionGroupNode == null)
					{
						XmlElement element = doc.CreateElement("sectionGroup");
						attribute = doc.CreateAttribute("name");
						attribute.Value = m_groupName;
						element.Attributes.Append(attribute);			
						sectionGroupNode = sectionsNode.AppendChild(element);			
					}
				}
	
				sectionNode = sectionGroupNode.SelectSingleNode("section[@name=\"" + section + "\"]");
				if (sectionNode == null)
				{
					XmlElement element = doc.CreateElement("section");
					attribute = doc.CreateAttribute("name");
					attribute.Value = section;
					element.Attributes.Append(attribute);			
	
					sectionNode = sectionGroupNode.AppendChild(element);			
				}
	
				attribute = doc.CreateAttribute("type");
				attribute.Value = SECTION_TYPE;
				sectionNode.Attributes.Append(attribute);			
			}

			XmlNode groupNode = root;
			if (hasGroupName)
			{
				groupNode = root.SelectSingleNode(m_groupName);
				if (groupNode == null)
					groupNode = root.AppendChild(doc.CreateElement(m_groupName));			
			}

			sectionNode = groupNode.SelectSingleNode(section);
			if (sectionNode == null)
				sectionNode = groupNode.AppendChild(doc.CreateElement(section));			

			XmlNode entryNode = sectionNode.SelectSingleNode("add[@key=\"" + entry + "\"]");
			if (entryNode == null)
			{
				XmlElement element = doc.CreateElement("add");
				attribute = doc.CreateAttribute("key");
				attribute.Value = entry;
				element.Attributes.Append(attribute);			

				entryNode = sectionNode.AppendChild(element);			
			}

			attribute = doc.CreateAttribute("value");
			attribute.Value = value.ToString();
			entryNode.Attributes.Append(attribute);			

			Save(doc);
			RaiseChangeEvent(false, ProfileChangeType.SetValue, section, entry, value);
		}

		public override object GetValue(string section, string entry)
		{
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);

			try
			{
				XmlDocument doc = GetXmlDocument();
				XmlElement root = doc.DocumentElement;				
				
				XmlNode entryNode = root.SelectSingleNode(GroupNameSlash + section + "/add[@key=\"" + entry + "\"]");
				return entryNode.Attributes["value"].Value;
			}
			catch
            {				
				return null;
			}
		}

		public override void RemoveEntry(string section, string entry)
		{
			VerifyNotReadOnly();
			VerifyAndAdjustSection(ref section);
			VerifyAndAdjustEntry(ref entry);

			XmlDocument doc = GetXmlDocument();
			if (doc == null)
				return;

			XmlElement root = doc.DocumentElement;			
			XmlNode entryNode = root.SelectSingleNode(GroupNameSlash + section + "/add[@key=\"" + entry + "\"]");
			if (entryNode == null)
				return;

			if (!RaiseChangeEvent(true, ProfileChangeType.RemoveEntry, section, entry, null))
				return;
			
			entryNode.ParentNode.RemoveChild(entryNode);			
			Save(doc);
			RaiseChangeEvent(false, ProfileChangeType.RemoveEntry, section, entry, null);
		}

		public override void RemoveSection(string section)
		{
			VerifyNotReadOnly();
			VerifyAndAdjustSection(ref section);

			XmlDocument doc = GetXmlDocument();
			if (doc == null)
				return;

			XmlElement root = doc.DocumentElement;
			if (root == null)
				return;

			XmlNode sectionNode = root.SelectSingleNode(GroupNameSlash + section);
			if (sectionNode == null)
				return;
			
			if (!RaiseChangeEvent(true, ProfileChangeType.RemoveSection, section, null, null))
				return;
			
			sectionNode.ParentNode.RemoveChild(sectionNode);

			if (!IsAppSettings(section))
			{											
				sectionNode = root.SelectSingleNode("configSections/" + (HasGroupName ? ("sectionGroup[@name=\"" + m_groupName + "\"]") : "") + "/section[@name=\"" + section + "\"]");
				if (sectionNode == null)
					return;
			
				sectionNode.ParentNode.RemoveChild(sectionNode);
			}
			
			Save(doc);
			RaiseChangeEvent(false, ProfileChangeType.RemoveSection, section, null, null);
		}

		public override string[] GetEntryNames(string section)
		{
			if (!HasSection(section))
				return null;
			    			
			VerifyAndAdjustSection(ref section);
			XmlDocument doc = GetXmlDocument();
			XmlElement root = doc.DocumentElement;
			
			XmlNodeList entryNodes = root.SelectNodes(GroupNameSlash + section + "/add[@key]");
			if (entryNodes == null)
				return null;

			string[] entries = new string[entryNodes.Count];
			int i = 0;
			
			foreach (XmlNode node in entryNodes)
				entries[i++] = node.Attributes["key"].Value;
			
			return entries;
		}
		
		public override string[] GetSectionNames()
		{
			XmlDocument doc = GetXmlDocument();
			if (doc == null)
				return null;

			XmlElement root = doc.DocumentElement;
			if (root == null)
				return null;

			XmlNode groupNode = (HasGroupName ? root.SelectSingleNode(m_groupName) : root);
			if (groupNode == null)
				return null;

			XmlNodeList sectionNodes = groupNode.ChildNodes;
			if (sectionNodes == null)
				return null;

			string[] sections = new string[sectionNodes.Count];			
			int i = 0;

			foreach (XmlNode node in sectionNodes)
				sections[i++] = node.Name;
			
			return sections;
		}		
	}
}
