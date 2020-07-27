using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CompanyEmployees.ActionFilters;
using Contracts;
using Entities.DataTransferObjects;
using Entities.LinkModels;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace CompanyEmployees.Controllers
{
    [Route("api/companies/{companyId}/employees")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IDataShaper<EmployeeDto> _dataShaper;
        private readonly IRepositoryManager _repository;
        private readonly LinkGenerator _linkGenerator;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;

        public EmployeesController(
            IDataShaper<EmployeeDto> dataShaper,
            IRepositoryManager repository,
            LinkGenerator linkGenerator,
            ILoggerManager logger,
            IMapper mapper
        )
        {
            _linkGenerator = linkGenerator;
            _dataShaper = dataShaper;
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [ServiceFilter(typeof(ValidateMediaTypeAttribute))]
        public async Task<IActionResult> GetEmployeesForCompany(Guid companyId, [FromQuery] EmployeeParameters employeeParameters)
        {
            if (!employeeParameters.ValidAgeRange)
                return BadRequest("Max age should be greater than min age.");

            var company = await _repository.Company
                .GetCompanyAsync(companyId, trackChanges: false);

            if (company == null)
            {
                _logger.LogError($"Company with ID = {companyId} not found.");
                return NotFound();
            }

            var employeesInDb = await _repository.Employee
                .GetEmployeesAsync(companyId, employeeParameters, trackChanges: false);
            
            Response.Headers.Add(
                "X-Pagination",
                JsonConvert.SerializeObject(employeesInDb.MetaData));

            var employeesDto = _mapper.Map<IEnumerable<EmployeeDto>>(employeesInDb);
            GenerateLinks(employeesDto, companyId);

            return Ok(_dataShaper.ShapeData(employeesDto, employeeParameters.Fields));
        }

        [HttpGet("{id}", Name = "GetEmployeeForCompany")]
        public IActionResult GetEmployeeForCompany(Guid companyId, Guid id)
        {
            var companyInDb = _repository.Company.GetCompanyAsync(companyId, trackChanges: false);

            if (companyInDb == null)
            {
                _logger.LogError($"Company with ID = {companyId} is not found in DB.");
                return NotFound();
            }

            var employeeInDb = _repository.Employee.GetEmployee(companyId, id, trackChanges: false);

            if (employeeInDb == null)
            {
                _logger.LogError($"Employee with ID = {id} is not found in DB.");
                return NotFound();
            }
            var employeeDto = _mapper.Map<EmployeeDto>(employeeInDb);

            return Ok(employeeDto);
        }
    
        [HttpPost]
        public IActionResult CreateEmployeeForCompany(Guid companyId, [FromBody] EmployeeForCreationDto employee)
        {
            if (employee == null)
            {
                _logger.LogError("EmployeeForCreationDto object sent from client is null.");
                return BadRequest("EmployeeForCreationDto object is null");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid model state for the EmployeeForCreationDto object");
                return UnprocessableEntity(ModelState);
            }

            var company = _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if(company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }
            var employeeEntity = _mapper.Map<Employee>(employee);
            _repository.Employee.CreateEmployeeForCompany(companyId, employeeEntity);
            _repository.SaveAsync();

            var employeeToReturn = _mapper.Map<EmployeeDto>(employeeEntity);
            return CreatedAtRoute(
                "GetEmployeeForCompany",
                new { companyId, id = employeeEntity.Id },
                employeeToReturn);
        }
    
        [HttpDelete("{id}", Name = "DeleteEmployeeForCompany")]
        public IActionResult DeleteEmployeeForCompany(Guid companyId, Guid id)
        {
            var company = _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }

            var employeeForCompany = _repository.Employee.GetEmployee(companyId, id, trackChanges: false);
            if (employeeForCompany == null)
            {
                _logger.LogInfo($"Employee with id: {id} doesn't exist in the database.");
                return NotFound();
            }

            _repository.Employee.DeleteEmployee(employeeForCompany);
            _repository.SaveAsync();
            return NoContent();
        }

        [HttpPut("{id}", Name = "UpdateEmployeeForCompany")]
        public IActionResult UpdateEmployeeForCompany(Guid companyId, Guid id, [FromBody] EmployeeForUpdateDto employee)
        {
            if (employee == null)
            {
                _logger.LogError("EmployeeForUpdateDto object sent from client is null.");
                return BadRequest("EmployeeForUpdateDto object is null");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid model state for the EmployeeForUpdateDto object");
                return UnprocessableEntity(ModelState);
            }

            var company = _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }

            var employeeEntity = _repository.Employee.GetEmployee(companyId, id, trackChanges: true);
            if (employeeEntity == null)
            {
                _logger.LogInfo($"Employee with id: {id} doesn't exist in the database.");
                return NotFound();
            }

            _mapper.Map(employee, employeeEntity);
            _repository.SaveAsync();

            return NoContent();
        }

        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateEmployeeForCompany(
            Guid companyId,
            Guid id,
            [FromBody] JsonPatchDocument<EmployeeForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                _logger.LogError("patchDoc object sent from client is null.");
                return BadRequest("patchDoc object is null");
            }

            var company = _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }

            var employeeEntity = _repository.Employee.GetEmployee(companyId, id, trackChanges: true);
            if (employeeEntity == null)
            {
                _logger.LogInfo($"Employee with id: {id} doesn't exist in the database.");
                return NotFound();
            }

            var employeeToPatch = _mapper.Map<EmployeeForUpdateDto>(employeeEntity);

            patchDoc.ApplyTo(employeeToPatch);

            TryValidateModel(employeeToPatch);

            if (! ModelState.IsValid)
            {
                _logger.LogError("Invalid model state for the patch document");
                return UnprocessableEntity(ModelState);
            }

            _mapper.Map(employeeToPatch, employeeEntity);

            _repository.SaveAsync();
            return NoContent();
        }
    
        private void GenerateLinks(IEnumerable<EmployeeDto> employeeDtos, Guid companyId)
        {
            foreach (var employee in employeeDtos)
            {
                var links = new List<Link>
                {
                    new Link
                    {
                        Href = _linkGenerator.GetUriByAction(HttpContext, "GetEmployeeForCompany", values: new { companyId, employee.Id }),
                        Rel = "self",
                        Method = "GET"
                    },
                    new Link
                    {
                        Href = _linkGenerator.GetUriByAction(HttpContext, "DeleteEmployeeForCompany", values: new { companyId, employee.Id }),
                        Rel = "delete_employee",
                        Method = "DELETE"
                    },
                    new Link
                    {
                        Href = _linkGenerator.GetUriByAction(HttpContext, "UpdateEmployeeForCompany", values: new { companyId, employee.Id }),
                        Rel = "update_employee",
                        Method = "PUT"
                    }
                };
                employee.Links = links;
            }
        }
    }
}