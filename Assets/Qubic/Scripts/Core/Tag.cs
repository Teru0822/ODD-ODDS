using System;
using System.Collections.Generic;

namespace QubicNS
{
    public partial class Tag
    {
        public string Name;
        public int Priority => 0;// means priority of edges marked with the tag
        public bool ProposeInUI => true;

        internal UInt64 mask;
        QubicBuilder Builder;

        public void Prepere(QubicBuilder builder)
        {
            mask = 0;
            Builder = builder;
        }

        public UInt64 Mask => mask == 0 ? mask = Builder.TagsMapper.GetOrCreate(Name) : mask;

        public static implicit operator UInt64(Tag tag) => tag.Mask;
    }

    public partial class BaseTagSet<T>
    {
        public static void Prepare(QubicBuilder builder)
        {
            foreach (var tag in GetTags())
                tag.Prepere(builder);
        }

        static BaseTagSet()
        {
            var tags = TypesHelper.GetStaticFieldsOfType<Tag>(typeof(T));
            foreach (var fi in tags)
            {
                var tag = (Tag)fi.GetValue(null);
                if (tag == null)
                {
                    tag = new Tag();
                    fi.SetValue(null, tag);
                }

                if (tag.Name.IsNullOrEmpty())
                    tag.Name = fi.Name;
            }
        }

        public static IEnumerable<Tag> GetTags()
        {
            return TypesHelper.GetStaticFieldValuesOfType<Tag>(typeof(T));
        }
    }
}