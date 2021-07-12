using System;
using System.Xml;

namespace Server.Accounting
{
    public class AccountComment
    {
        private string m_Content;

        /// <summary>
        ///     Constructs a new AccountComment instance.
        /// </summary>
        /// <param name="addedBy">Initial AddedBy value.</param>
        /// <param name="content">Initial Content value.</param>
        public AccountComment(string addedBy, string content)
        {
            AddedBy = addedBy;
            m_Content = content;
            LastModified = Core.Now;
        }

        /// <summary>
        ///     Deserializes an AccountComment instance from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        public AccountComment(XmlElement node)
        {
            AddedBy = Utility.GetAttribute(node, "addedBy", "empty");
            LastModified = Utility.GetXMLDateTime(Utility.GetAttribute(node, "lastModified"), Core.Now);
            m_Content = Utility.GetText(node, "");
        }

        /// <summary>
        ///     Deserializes an AccountComment instance.
        /// </summary>
        /// <param name="node">The deserialization reader</param>
        public AccountComment(IGenericReader reader)
        {
            AddedBy = reader.ReadString();
            LastModified = reader.ReadDateTime();
            m_Content = reader.ReadString();
        }

        /// <summary>
        ///     A string representing who added this comment.
        /// </summary>
        public string AddedBy { get; }

        /// <summary>
        ///     Gets or sets the body of this comment. Setting this value will reset LastModified.
        /// </summary>
        public string Content
        {
            get => m_Content;
            set
            {
                m_Content = value;
                LastModified = Core.Now;
            }
        }

        /// <summary>
        ///     The date and time when this account was last modified -or- the comment creation time, if never modified.
        /// </summary>
        public DateTime LastModified { get; private set; }

        /// <summary>
        ///     Serializes this AccountComment instance.
        /// </summary>
        /// <param name="xml">The serialization writer.</param>
        public void Serialize(IGenericWriter writer)
        {
            writer.Write(AddedBy ?? "empty");
            writer.Write(LastModified);
            writer.Write(m_Content);
        }
    }
}
