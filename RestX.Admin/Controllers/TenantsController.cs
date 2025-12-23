using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;

namespace RestX.Admin.Controllers
{
    public class TenantsController : Controller
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public async Task<IActionResult> Index()
        {
            var tenants = await _tenantService.GetTenantsAsync();
            return View(tenants);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tenant = await _tenantService.GetTenantByIdAsync(id.Value);
            if (tenant == null)
            {
                return NotFound();
            }

            return View(tenant);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Prefix,Name,LogoUrl,FaviconUrl,BackgroundUrl,BaseColor,PrimaryColor,SecondaryColor,NetworkIp,ConnectionString,Status,Domain,ExpiredAt,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy")] Tenant tenant)
        {
            if (ModelState.IsValid)
            {
                await _tenantService.UpsertTenantAsync(tenant);
                return RedirectToAction(nameof(Index));
            }
            return View(tenant);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tenant = await _tenantService.GetTenantByIdAsync(id.Value);
            if (tenant == null)
            {
                return NotFound();
            }
            return View(tenant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Prefix,Name,LogoUrl,FaviconUrl,BackgroundUrl,BaseColor,PrimaryColor,SecondaryColor,NetworkIp,ConnectionString,Status,Domain,ExpiredAt,Id,CreatedDate,ModifiedDate,CreatedBy,ModifiedBy")] Tenant tenant)
        {
            if (id != tenant.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _tenantService.UpsertTenantAsync(tenant);
                }
                catch (Exception)
                {
                    if (!await TenantExistsAsync(tenant.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tenant);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tenant = await _tenantService.GetTenantByIdAsync(id.Value);
            if (tenant == null)
            {
                return NotFound();
            }

            return View(tenant);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _tenantService.DeleteTenantAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> TenantExistsAsync(Guid id)
        {
            var tenant = await _tenantService.GetTenantByIdAsync(id);
            return tenant != null;
        }
    }
}