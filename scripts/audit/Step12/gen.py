import json,re,collections,sys
from audit import *
gm=json.load(open('global_members.json')); dead=set(json.load(open('global_dead.json')))
r={x['name']:x for x in json.load(open('global_counts.json'))}
gp=os.path.normpath(os.path.join(ROOT,'DS4Windows/DS4Control/Global.cs'))
gl=SRC[gp].split('\n')
inst={'profileRepository','fallbackProfileRepository','ProfileRepositoryInstance','specialActionRepository','fallbackSpecialActionRepository','SpecialActionRepositoryInstance','deviceStateService','fallbackDeviceStateService','DeviceStateServiceInstance','outputSlotService','fallbackOutputSlotService','CreateFallbackOutputSlotService','OutputSlotServiceInstance','pathService','fallbackPathService','PathServiceInstance','environmentService','fallbackEnvironmentService','EnvironmentServiceInstance'}
orph={'m_IdleTimeout','exeFileName','exeversionLong','fullExeVersionLong','localAppDataPpath','linkedProfileCheck','DS4Mapping','DownloadLang','CheckForDevice','ControllerStatusChange','ControllerStatusChanged','BatteryStatusChange','OnBatteryStatusChange','ControllerRemoved','OnControllerRemoved','DeviceStatusChange','OnDeviceStatusChanged','getUseExclusiveMode','getQuickCharge','GetMinToTaskbar','getFlashWhenLate','getFlashWhenLateAt','IsUsingUDPServerSmoothing','DebouncingMsHasChanged','NormalizeActionName'}
judge={'DS4_STICK_AXIS_MIDPOINT','getDefaultX360ControlBinding'}
W=collections.defaultdict(list)
for m in gm:
    n=m['name']
    if n not in dead or n in judge: continue
    txt='\n'.join(gl[m['line']-1:m['end']])
    if n in inst: w='W3'
    elif n in orph: w='W1'
    elif 'ProfileSettingsServiceInstance' in txt: w='W2'
    else: w='W4'
    W[w].append(n)
out={}
for w,v in W.items():
    seen=[]; 
    for n in v:
        if n not in seen: seen.append(n)
    out[w]=[(n, r[n]['q_test']) for n in seen]
    out[w+'_decl']=len(v)
json.dump(out,open('waves.json','w'),ensure_ascii=False,indent=1)
for w in ['W1','W2','W3','W4']: print(w,out[w+'_decl'],len(out[w]))
def fmt(lst): return '、'.join(f"`{n}`" + (f"（テスト{t}）" if t else '') for n,t in lst)
open('wave_text.md','w',encoding='utf-8').write('\n'.join(f"W{i}\n{fmt(out['W'+str(i)])}\n" for i in range(1,5)))
