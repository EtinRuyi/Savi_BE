﻿using Microsoft.AspNetCore.Mvc;
using Savi_Thrift.Application.DTO.Group;
using Savi_Thrift.Application.Interfaces.Services;
using TicketEase.Domain;

namespace Savi_Thrift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost("Add-Groups")]
        public async Task<IActionResult> CreateGroup([FromBody] GroupCreationDto groupCreationDto)
        {
            if (groupCreationDto == null)
            {
                return BadRequest("Invalid group data");
            }

            var response = await _groupService.CreateGroupAsync(groupCreationDto);

            return response.Succeeded
                ? Created($"api/groups/{response.Data.Name}", response)
                : StatusCode(response.StatusCode, response);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetGroupById(string id)
        {
            var response = await _groupService.GetGroupByIdAsync(id);

            if (response != null && response.Succeeded)
            {
                return Ok(response);
            }

            return NotFound(response?.Errors ?? new List<string> { "Error retrieving group" });
        }

        [HttpGet]
        [Route("all")]
        public async Task<IActionResult> GetAllGroups()
        {
            var response = await _groupService.GetAllGroupsAsync();

            if (response.Succeeded)
            {
                return Ok(response);
            }

            return StatusCode(response.StatusCode, response);
        }

        [HttpPut]
        [Route("updatePhoto/{id}")]
        public async Task<IActionResult> UpdateGroupPhotoById(string id, [FromForm] UpdateGroupPhotoDto model)
        {
            var imageUrl = await _groupService.UpdateGroupPhotoByGroupId(id, model);

            if (imageUrl != null)
            {
                return Ok(new ApiResponse<string>(true, "Group photo updated successfully", 200, imageUrl, null));
            }

            return StatusCode(500, new ApiResponse<string?>(false, "Failed to update group photo", 500, null, null));
        }
    }
}