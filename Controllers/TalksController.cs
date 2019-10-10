using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace CoreCodeCamp.Controllers
{
    [Route("api/camps/{moniker}/talks")]
    [ApiController]
    public class TalksController : ControllerBase
    {
        private readonly ICampRepository _repository;
        private readonly IMapper _mapper;
        private readonly LinkGenerator _linkGenerator;

        public TalksController(ICampRepository repository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _repository = repository;
            _mapper = mapper;
            _linkGenerator = linkGenerator;
        }

        [HttpGet]
        public async Task<ActionResult<TalkModel[]>> Get(string moniker, bool includeSpeaker = false)
        {
            try
            {
                var talks = await _repository.GetTalksByMonikerAsync(moniker, includeSpeaker);
                return _mapper.Map<TalkModel[]>(talks);
            }
            catch (Exception ex)
            {

                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }

        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TalkModel>> Get(string moniker, int id, bool includeSpeaker = false)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker, id, includeSpeaker);
                if (talk==null)
                {
                    return NotFound("Talk not found");
                }
                return _mapper.Map<TalkModel>(talk);
            }
            catch (Exception)
            {

                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }

        }

        [HttpPost]
        public async Task<ActionResult<TalkModel>> Post(string moniker, TalkModel model)
        {
            try
            {
                var camp = await _repository.GetCampAsync(moniker);
                if (camp == null)
                {
                    return BadRequest("Camp does not exit");
                }

                var talk = _mapper.Map<Talk>(model);
                talk.Camp = camp;

                if (model.speaker == null)
                {
                    return BadRequest("Speaker ID is required");
                }

                var speaker = await _repository.GetSpeakerAsync(model.speaker.SpeakerId);

                if (speaker == null)
                {
                    return BadRequest("Speaker not found");
                }

                talk.Speaker = speaker;
                _repository.Add(talk);

                if (await _repository.SaveChangesAsync())
                {
                    var url = _linkGenerator.GetPathByAction(HttpContext, "Get", values: new { moniker, id = talk.TalkId });
                    return Created(url, _mapper.Map<TalkModel>(talk));
                }
                else
                {
                    return BadRequest("Failed to save new talk");
                }
            }
            catch (Exception)
            {

                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }

        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TalkModel>> Put(string moniker,int id,TalkModel model)
        {
            try
            {
                var oldTalk = await _repository.GetTalkByMonikerAsync(moniker,id,true);

                if (oldTalk == null)
                {
                    return NotFound("Talk no found");
                }

                if (model.speaker!=null)
                {
                    var speaker = await _repository.GetSpeakerAsync(model.speaker.SpeakerId);
                    if (speaker!=null)
                    {
                        oldTalk.Speaker = speaker;
                    }
                }
                _mapper.Map(model, oldTalk);

                if (await _repository.SaveChangesAsync())
                {
                    return _mapper.Map<TalkModel>(oldTalk);
                }
            }
            catch (Exception)
            {

                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }

            return BadRequest();

        } 

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete (string moniker, int id)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker, id);
                if (talk !=null)
                {
                    return NotFound("Talk not found");
                }

                _repository.Delete(talk);

                if (await _repository.SaveChangesAsync())
                {
                    return Ok();
                }

            }
            catch (Exception)
            {

                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }

            return BadRequest();
        }
    }
}
