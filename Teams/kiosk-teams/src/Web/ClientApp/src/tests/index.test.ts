/**
 * @jest-environment jsdom
 */

import { AppContentManager } from "../engine/AppContentManager";
import { getTeamsMeetingDetails, getTeamsMeetingDetailsParam } from "../engine/TeamsMeetingUrlParser";
import { FakeAppContentLoader } from "./FakeAppContentLoader";


describe('AppContentLoader tests', () => {
  test('AppContentLoader', async () => {

    const loader = new FakeAppContentLoader();
    const url = "http://whatever/" + new Date().toISOString();
    loader.setNextUrl(url)

    const m = new AppContentManager("Unit tests", 10, loader);
    let lastItem: PlayListItem | null = null;
    let callbackCount: number = 0;
    m.start((url: PlayListItem) => 
    {
      lastItem = url;
      callbackCount++;
    });
    await sleep(100);
    expect(lastItem !== null).toBeTruthy();
    expect(lastItem!.url === url).toBeTruthy();
    expect(callbackCount).toEqual(1);
  });

  test('TeamsMeetingDetails encoding', async () => {

    const meetingUrl = "1232"
    const encodedUrl = getTeamsMeetingDetailsParam({activateAcsClientWebCam: true, activateAcsClientMic: false, joinUrl: meetingUrl});
    
    expect(encodedUrl && encodedUrl.length > 0).toBeTruthy();
    const decoded = getTeamsMeetingDetails(encodedUrl);
    
    expect(decoded!.joinUrl === meetingUrl).toBeTruthy();

    const nullResult = getTeamsMeetingDetails("123");
    expect(nullResult).toBeNull();
  });
});

async function sleep(msec: number) {
  return new Promise(resolve => setTimeout(resolve, msec));
}
