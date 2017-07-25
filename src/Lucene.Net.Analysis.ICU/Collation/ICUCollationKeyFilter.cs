﻿using Icu.Collation;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Globalization;

namespace Lucene.Net.Collation
{
    /// <summary>
    /// Converts each token into its <see cref="SortKey"/>, and
    /// then encodes the <see cref="SortKey"/> with <see cref="IndexableBinaryStringTools"/>, to
    /// allow it to be stored as an index term.
    /// </summary>
    /// <remarks>
    /// <strong>WARNING:</strong> Make sure you use exactly the same <see cref="Collator"/> at
    /// index and query time -- CollationKeys are only comparable when produced by
    /// the same <see cref="Collator"/>.  <see cref="RuleBasedCollator"/>s are 
    /// independently versioned, so it is safe to search against stored
    /// <see cref="System.Globalization.SortKey"/>s if the following are exactly the same (best practice is
    /// to store this information with the index and check that they remain the
    /// same at query time):
    /// <list type="number">
    ///     <item><description>Collator version - see <see cref="Collator"/> Version</description></item>
    ///     <item><description>The collation strength used - see <see cref="Collator.Strength"/></description></item>
    /// </list>
    /// <para/>
    /// <see cref="System.Globalization.SortKey"/>s generated by ICU Collators are not compatible with those
    /// generated by java.text.Collators.  Specifically, if you use 
    /// <see cref="ICUCollationKeyAnalyzer"/> to generate index terms, do not use 
    /// CollationKeyAnalyzer on the query side, or vice versa.
    /// <para/>
    /// ICUCollationKeyAnalyzer is significantly faster and generates significantly
    /// shorter keys than CollationKeyAnalyzer.  See
    /// <a href="http://site.icu-project.org/charts/collation-icu4j-sun"
    /// >http://site.icu-project.org/charts/collation-icu4j-sun</a> for key
    /// generation timing and key length comparisons between ICU4J and
    /// java.text.Collator over several languages.
    /// </remarks>
    [Obsolete("Use ICUCollationAttributeFactory instead, which encodes terms directly as bytes. This filter will be removed in Lucene 5.0")]
    [ExceptionToClassNameConvention]
    public sealed class ICUCollationKeyFilter : TokenFilter
    {
        private Collator collator = null;
        private SortKey reusableKey;
        private readonly ICharTermAttribute termAtt;

        /// <summary>
        /// Creates a new <see cref="ICUCollationKeyFilter"/>.
        /// </summary>
        /// <param name="input">Source token stream.</param>
        /// <param name="collator"><see cref="SortKey"/> generator.</param>
        public ICUCollationKeyFilter(TokenStream input, Collator collator)
            : base(input)
        {
            // clone the collator: see http://userguide.icu-project.org/collation/architecture
            this.collator = (Collator)collator.Clone();
            this.termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                char[] termBuffer = termAtt.Buffer;
                string termText = new string(termBuffer, 0, termAtt.Length);
                reusableKey = collator.GetSortKey(termText);
                int encodedLength = IndexableBinaryStringTools.GetEncodedLength(
                    reusableKey.KeyData, 0, reusableKey.KeyData.Length);
                if (encodedLength > termBuffer.Length)
                {
                    termAtt.ResizeBuffer(encodedLength);
                }
                termAtt.SetLength(encodedLength);
                IndexableBinaryStringTools.Encode(reusableKey.KeyData, 0, reusableKey.KeyData.Length,
                    termAtt.Buffer, 0, encodedLength);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
