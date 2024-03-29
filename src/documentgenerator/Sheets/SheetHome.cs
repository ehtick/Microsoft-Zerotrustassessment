﻿using Syncfusion.XlsIO;
using ZeroTrustAssessment.DocumentGenerator.Graph;
using ZeroTrustAssessment.DocumentGenerator.Infrastructure;
using static System.Formats.Asn1.AsnWriter;

namespace ZeroTrustAssessment.DocumentGenerator.Sheets;

public class SheetHome : SheetBase
{
    public SheetHome(IWorkbook workbook, ZtSheets sheet, GraphData graphData) : base(workbook, sheet, graphData)
    {
    }

    public void Generate(AssessmentScore identityScore, AssessmentScore deviceScore)
    {
        SetDocHeaderInfo();
        ShowDashboard(identityScore, deviceScore);
    }

    private void ShowDashboard(AssessmentScore identityScore, AssessmentScore deviceScore)
    {
        ShowScore("identityScore", identityScore);
        ShowScore("deviceScore", deviceScore);
    }

    private void SetDocHeaderInfo()
    {
        var currentDate = DateTime.Now.ToString("dd MMM yyyy");
        _sheet.Range[ExcelConstant.HomeHeaderTenantIdLabel].Text = $"Tenant ID: {_graphData.TenantId}";
        _sheet.Range["HeaderTitle"].Text = $"Zero Trust Assessment for {_graphData.TenantName}";
        _sheet.Range["HeaderAssessedOn"].Text = $"Assessment generated on {currentDate} by {_graphData?.Me?.DisplayName} ({_graphData?.Me?.UserPrincipalName})";

        SetBannerImage("bannerLogoBg", true);
    } 

    private void SetBannerImage(string shapeName, bool hasBackground = false)
    {
        var logoBackgroundRectangle = _sheet.Shapes["bannerLogoBg"]; //Get the logo background

        if(logoBackgroundRectangle == null) return;
        
        if (_graphData.OrganizationLogo != null)
        {
            var picture = _sheet.Pictures.AddPicture(1, 1, _graphData.OrganizationLogo);
            picture.Top = 10;
            picture.Left = 25;
            picture.Width = picture.Width * 30 / 100;
            picture.Height = picture.Height * 30 / 100;
            picture.AlternativeText = "Organization banner logo";
            //Position the background behind the logo
            logoBackgroundRectangle.Top = picture.Top - 5;
            logoBackgroundRectangle.Left = picture.Left - 5;
            logoBackgroundRectangle.Width = picture.Width + 10;
            logoBackgroundRectangle.Height = picture.Height + 10;
        }
        else
        {
            logoBackgroundRectangle.Remove(); //No logo image, remove the background as well
        }
    }
}

