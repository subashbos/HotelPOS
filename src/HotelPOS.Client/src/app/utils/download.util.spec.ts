import { downloadBlob } from './download.util';

describe('downloadBlob', () => {
  it('creates an object URL, clicks a download anchor, then revokes the URL', () => {
    const createObjectURLSpy = spyOn(window.URL, 'createObjectURL').and.returnValue('blob:mock-url');
    const revokeObjectURLSpy = spyOn(window.URL, 'revokeObjectURL');
    const clickSpy = spyOn(HTMLAnchorElement.prototype, 'click');

    const blob = new Blob(['data']);
    downloadBlob(blob, 'Report.xlsx');

    expect(createObjectURLSpy).toHaveBeenCalledWith(blob);
    expect(clickSpy).toHaveBeenCalled();
    expect(revokeObjectURLSpy).toHaveBeenCalledWith('blob:mock-url');
  });
});
