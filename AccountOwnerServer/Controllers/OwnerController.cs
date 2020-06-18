using Contracts;
using Entities.Extensions;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using Entities.Dtos;
using AutoMapper;
using System.Collections.Generic;

namespace AccountOwnerServer.Controllers
{
    [Authorize]
    [Route("api/owner")]
    [ApiController]
    public class OwnerController : ControllerBase
    {
        private readonly ILoggerManager _logger;
        private readonly IUserService _userService;
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;

        public OwnerController(ILoggerManager logger, IUserService userService, IRepositoryWrapper repository, IMapper mapper)
        {
            _logger = logger;
            _userService = userService;
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetAllOwners()
        {
            try
            {
                var owners = _repository.Owner.GetAllOwners();

                var ownerDto = _mapper.Map<OwnerDto>(owners.ToArray()[0]);

                List<OwnerDto> ownersDto = new List<OwnerDto>();

                foreach (var owner in owners)
                {
                    ownersDto.Add(_mapper.Map<OwnerDto>(owner));
                }

                //var ownerDtoss = _mapper.Map<List<OwnerDto>>(owners);

                _logger.LogInfo($"Returned all owners from database.");

                return Ok(ownersDto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetAllOwners action: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}", Name = "OwnerById")]
        public IActionResult GetOwnerById(Guid id)
        {
            try
            {
                var owner = _repository.Owner.GetOwnerById(id);

                if (owner.Id.Equals(Guid.Empty))
                {
                    _logger.LogError($"Owner with id: {id}, hasn't been found in db.");
                    return NotFound();
                }
                else
                {
                    _logger.LogInfo($"Returned owner with id: {id}");
                    return Ok(owner);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetOwnerById action: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/account")]
        public IActionResult GetOwnerWithDetails(Guid id)
        {
            try
            {
                var owner = _repository.Owner.GetOwnerWithDetails(id);

                if (owner.IsEmptyObject())
                {
                    _logger.LogError($"Owner with id: {id}, hasn't been found in db.");
                    return NotFound();
                }
                else
                {
                    _logger.LogInfo($"Returned owner with details for id: {id}");
                    return Ok(owner);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside GetOwnerWithDetails action: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public IActionResult CreateOwner([FromBody] Owner owner)
        {
            try
            {
                if (owner.IsObjectNull())
                {
                    _logger.LogError("Owner object sent from client is null.");
                    return BadRequest("Owner object is null");
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogError("Invalid owner object sent from client.");
                    return BadRequest("Invalid model object");
                }

                _repository.Owner.CreateOwner(owner);
                _repository.Save();

                return CreatedAtRoute("OwnerById", new { id = owner.Id }, owner);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside CreateOwner action: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] OwnerDto ownerDto)
        {
            var user = _userService.Authenticate(ownerDto.Email, ownerDto.Password);

            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            var tokenString = _userService.GetJWTToken(user.Id);
            var refreshtokenString = _userService.GetJWTRefreshToken(ipAddress());

            user.RefreshToken = refreshtokenString;

            _userService.Update(user);

            // return basic user info (without password) and token to store client side
            /*return Ok(new
            {
                Id = user.Id,
                Name = user.Name,
                Token = tokenString,
                RefreshToken = refreshtokenString
            });*/
            return Ok(new AuthenticateResponse(user, tokenString, refreshtokenString));
        }

        [AllowAnonymous]
        [HttpPost("refreshtoken")]
        public IActionResult RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            //var refreshToken = Request.Cookies["refreshToken"];
            var response = _userService.RefreshToken(refreshTokenDto.refreshToken, ipAddress());

            if (response == null)
                return Unauthorized(new { message = "Invalid token" });

            //setTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody]OwnerDto ownerDto)
        {
            // map dto to entity
            var owner = _mapper.Map<Owner>(ownerDto);

            try
            {
                // save 
                _userService.Create(owner, ownerDto.Password);
                return Ok();
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateOwner(Guid id, [FromBody]OwnerDto ownerDto)
        {
            try
            {
                /*if (ownerDto.IsObjectNull())
                {
                    _logger.LogError("Owner object sent from client is null.");
                    return BadRequest("Owner object is null");
                }*/

                if (!ModelState.IsValid)
                {
                    _logger.LogError("Invalid owner object sent from client.");
                    return BadRequest("Invalid model object");
                }

                var owner = _mapper.Map<Owner>(ownerDto);
                owner.Id = id;

                _userService.Update(owner, ownerDto.Password);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside UpdateOwner action: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteOwner(Guid id)
        {
            try
            {
                var owner = _userService.GetById(id);
                if (owner.IsEmptyObject())
                {
                    _logger.LogError($"Owner with id: {id}, hasn't been found in db.");
                    return NotFound();
                }

                if (_repository.Account.AccountsByOwner(id).Any())
                {
                    _logger.LogError($"Cannot delete owner with id: {id}. It has related accounts. Delete those accounts first");
                    return BadRequest("Cannot delete owner. It has related accounts. Delete those accounts first");
                }

                _userService.Delete(owner);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside DeleteOwner action: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        private string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
