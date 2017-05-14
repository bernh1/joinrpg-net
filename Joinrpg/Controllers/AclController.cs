﻿using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [MasterAuthorize()]
  public class AclController : ControllerGameBase
  { 
    private IClaimsRepository ClaimRepository { get; }
    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights, AllowAdmin = true) ]
    public async Task<ActionResult> Add(AclViewModel viewModel)
    {
      try
      {
        await ProjectService.GrantAccess(viewModel.ProjectId, CurrentUserId, viewModel.UserId, viewModel.CanGrantRights,
          viewModel.CanChangeFields, viewModel.CanChangeProjectProperties, viewModel.CanManageClaims,
          viewModel.CanEditRoles, viewModel.CanManageMoney, viewModel.CanSendMassMails, viewModel.CanManagePlots);
      }
      catch
      {
        //TODO Fix this.
        ModelState.AddModelError("", "Error!");
        return RedirectToAction("Details", "User", new {viewModel.UserId});
      }
      return RedirectToAction("Index", "Acl", new {viewModel.ProjectId});
    }

    public AclController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IClaimsRepository claimRepository)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      ClaimRepository = claimRepository;
    }

    [HttpGet]
    public async Task<ActionResult> Index(int projectId)
    {
      var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
      var claims = await ClaimRepository.GetClaimsCountByMasters(projectId, ClaimStatusSpec.Active);
      var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);
      return View(project.ProjectAcls.Select(acl =>
      {
        return AclViewModel.FromAcl(acl, claims.SingleOrDefault(c => c.MasterId == acl.UserId)?.ClaimCount ?? 0,
          groups.Where(gr => gr.ResponsibleMasterUserId == acl.UserId).ToList());
      }));
    }

    [HttpGet, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Delete(int projectId, int projectaclid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var projectAcl = project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid);
      var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);
      return View(AclViewModel.FromAcl(projectAcl, 0, groups.Where(gr => gr.ResponsibleMasterUserId == projectAcl.UserId).ToList()));

    }

    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Delete(AclViewModel viewModel)
    {
      try
      {
        await ProjectService.RemoveAccess(viewModel.ProjectId, CurrentUserId, viewModel.UserId);
      }
      catch
      {
        return View(viewModel);
      }
      return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

    }


    [HttpGet, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Edit(int projectId, int? projectaclid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var groups = await ProjectRepository.GetGroupsWithResponsible(projectId);
      var projectAcl = project.ProjectAcls.Single(acl => acl.ProjectAclId == projectaclid);
      return View(AclViewModel.FromAcl(projectAcl, 0, groups.Where(gr => gr.ResponsibleMasterUserId == projectAcl.UserId).ToList()));
    }

    [HttpPost, ValidateAntiForgeryToken, MasterAuthorize(Permission.CanGrantRights)]
    public async Task<ActionResult> Edit(AclViewModel viewModel)
    {
      try
      {
        await
          ProjectService.ChangeAccess(viewModel.ProjectId, CurrentUserId, viewModel.UserId, viewModel.CanGrantRights,
            viewModel.CanChangeFields, viewModel.CanChangeProjectProperties, viewModel.CanManageClaims,
            viewModel.CanEditRoles, viewModel.CanManageMoney, viewModel.CanSendMassMails, viewModel.CanManagePlots);
      }
      catch
      {
        return View(viewModel);
      }
      return RedirectToAction("Index", "Acl", new { viewModel.ProjectId });

    }
  }
}