<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:code="external:scripts"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                exclude-result-prefixes="msxsl code"
>
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="/">
    <Original>
      <xsl:apply-templates />
    </Original>
    <Result>
      <COMPLEX>
        <xsl:attribute name="counter">
          <xsl:value-of select="code:CurrentValue()"/>
        </xsl:attribute>
        <xsl:value-of select="code:ToUpper(//complex)" />
      </COMPLEX>
    </Result>
  </xsl:template>

  <xsl:template match="@* | node()">
    <xsl:variable name="counter">
      <xsl:value-of select="code:Increment()"/>
    </xsl:variable>
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
